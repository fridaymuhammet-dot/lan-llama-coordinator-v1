using LanLlamaCoordinator.Core.Contracts.Common;
using LanLlamaCoordinator.Core.Contracts.Tasks;
using LanLlamaCoordinator.Core.Contracts.Workers;

namespace LanLlamaCoordinator.Infrastructure.WorkerProtocol;

/// <summary>
/// In-memory protocol implementation for deterministic local tests. It provides no network transport,
/// authentication, durable storage, or production security boundary.
/// </summary>
public sealed class InMemoryWorkerCoordinatorProtocol : IWorkerCoordinatorProtocol
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(60);
    private readonly object _gate = new();
    private readonly Dictionary<string, PendingWorker> _registrations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, WorkerState> _workers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, WorkItem> _tasks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ClaimRecord> _claimRequests = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ResultRecord> _resultRequests = new(StringComparer.Ordinal);

    public WorkerRegistrationResponse Register(WorkerRegistrationRequest request)
    {
        if (!IsValid(request))
        {
            return new WorkerRegistrationResponse(string.Empty, WorkerRegistrationStatus.InvalidRequest,
                "Registration request is invalid.", new ProtocolError("invalid_registration", "Required fields or capability values are invalid."));
        }

        lock (_gate)
        {
            var existing = _registrations.Values.FirstOrDefault(x => x.Request.RequestId == request.RequestId);
            if (existing is not null)
            {
                return RegistrationResponse(existing);
            }

            var workerId = Guid.NewGuid().ToString("N");
            var pending = new PendingWorker(workerId, request);
            _registrations[workerId] = pending;
            return RegistrationResponse(pending);
        }
    }

    public WorkerApprovalResponse Approve(WorkerApprovalRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RequestId))
        {
            return new WorkerApprovalResponse(string.Empty, WorkerRegistrationStatus.InvalidRequest, "A request ID is required.");
        }

        lock (_gate)
        {
            var registration = _registrations.Values.FirstOrDefault(x => x.Request.RequestId == request.RequestId);
            if (registration is null)
            {
                return new WorkerApprovalResponse(string.Empty, WorkerRegistrationStatus.InvalidRequest, "Registration was not found.");
            }

            if (registration.Status != WorkerRegistrationStatus.PendingApproval)
            {
                var message = registration.Status == WorkerRegistrationStatus.Rejected
                    ? "A rejected registration cannot be approved; submit a new registration request."
                    : "Registration has already been approved.";
                return new WorkerApprovalResponse(registration.WorkerId, registration.Status, message);
            }

            if (!request.Approve)
            {
                registration.Status = WorkerRegistrationStatus.Rejected;
                registration.Message = string.IsNullOrWhiteSpace(request.Reason)
                    ? "Registration rejected by coordinator."
                    : request.Reason;
                return new WorkerApprovalResponse(registration.WorkerId, registration.Status, registration.Message);
            }

            registration.Status = WorkerRegistrationStatus.Approved;
            registration.Message = "Worker approved; waiting for its first heartbeat.";
            _workers[registration.WorkerId] = new WorkerState(registration.Request);
            return new WorkerApprovalResponse(registration.WorkerId, registration.Status, registration.Message);
        }
    }

    public WorkerHeartbeatResponse Heartbeat(WorkerHeartbeatRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.WorkerId) || string.IsNullOrWhiteSpace(request.RequestId))
        {
            return RejectedHeartbeat("invalid_heartbeat", "Worker ID and request ID are required.");
        }

        lock (_gate)
        {
            if (!_workers.TryGetValue(request.WorkerId, out var worker))
            {
                return RejectedHeartbeat("unknown_worker", "Worker is unknown, pending approval, or rejected.");
            }

            var now = DateTimeOffset.UtcNow;
            if (request.SentAtUtc > now.AddSeconds(30) || request.SentAtUtc < now.Subtract(HeartbeatTimeout))
            {
                return RejectedHeartbeat("invalid_timestamp", "Heartbeat timestamp is outside the accepted time window.");
            }

            if (request.SequenceNumber <= worker.LastSequenceNumber)
            {
                return RejectedHeartbeat("stale_sequence", "Heartbeat sequence number must increase.");
            }

            if (!Enum.IsDefined(request.Status) || !IsValid(request.CurrentCapacity))
            {
                return RejectedHeartbeat("invalid_capacity", "Heartbeat status or capacity values are invalid.");
            }

            worker.LastSequenceNumber = request.SequenceNumber;
            worker.LastHeartbeatUtc = now;
            worker.LastStatus = request.Status;
            worker.Capacity = request.CurrentCapacity;
            return new WorkerHeartbeatResponse(true, "Heartbeat accepted.", HeartbeatInterval);
        }
    }

    public bool Enqueue(EnqueueTaskRequest task)
    {
        if (task is null || string.IsNullOrWhiteSpace(task.TaskId) || string.IsNullOrWhiteSpace(task.ModelName) ||
            string.IsNullOrWhiteSpace(task.RequiredBackend) || task.RequiredMemoryBytes < 0 ||
            string.IsNullOrWhiteSpace(task.Prompt) || task.Prompt.Length > 32_000 || task.MaxTokens <= 0 || task.MaxAttempts <= 0)
        {
            return false;
        }

        lock (_gate)
        {
            if (_tasks.ContainsKey(task.TaskId))
            {
                return false;
            }

            _tasks.Add(task.TaskId, new WorkItem(task));
            return true;
        }
    }

    public TaskClaimResponse ClaimTask(TaskClaimRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.WorkerId) || string.IsNullOrWhiteSpace(request.RequestId) ||
            request.SupportedBackends is null || request.AvailableMemoryBytes < 0)
        {
            return ClaimError("invalid_claim", "Worker ID, request ID, backend list and non-negative memory are required.");
        }

        lock (_gate)
        {
            var claimKey = $"{request.WorkerId}:{request.RequestId}";
            if (_claimRequests.TryGetValue(claimKey, out var prior))
            {
                return prior.Response;
            }

            if (!_workers.TryGetValue(request.WorkerId, out var worker))
            {
                return ClaimError("unknown_worker", "Worker is unknown, pending approval, or rejected.");
            }

            if (worker.LastHeartbeatUtc is null || DateTimeOffset.UtcNow - worker.LastHeartbeatUtc > HeartbeatTimeout ||
                worker.LastStatus != WorkerRunStatus.Idle)
            {
                return ClaimError("worker_unavailable", "Worker has no recent idle heartbeat.");
            }

            var effectiveMemory = Math.Min(request.AvailableMemoryBytes, worker.Capacity.AvailableMemoryBytes);
            var item = _tasks.Values
                .Where(x => x.Status == TaskWorkStatus.Queued)
                .Where(x => worker.Registration.SupportedBackends.Contains(x.Request.RequiredBackend, StringComparer.OrdinalIgnoreCase))
                .Where(x => request.SupportedBackends.Contains(x.Request.RequiredBackend, StringComparer.OrdinalIgnoreCase))
                .Where(x => effectiveMemory >= x.Request.RequiredMemoryBytes)
                .OrderBy(x => x.CreatedUtc)
                .FirstOrDefault();

            if (item is null)
            {
                var noWork = new TaskClaimResponse(false, null, null, "No compatible task is available.");
                _claimRequests[claimKey] = new ClaimRecord(request.RequestId, noWork);
                return noWork;
            }

            item.Status = TaskWorkStatus.Leased;
            item.WorkerId = request.WorkerId;
            item.LeaseId = Guid.NewGuid().ToString("N");
            item.LeaseExpiresAtUtc = DateTimeOffset.UtcNow.Add(LeaseDuration);
            item.Attempt++;

            var response = new TaskClaimResponse(true,
                new ClaimedTaskDto(item.Request.TaskId, item.LeaseId, item.Attempt, item.Request.ModelName,
                    item.Request.RequiredBackend, item.Request.Prompt, item.Request.MaxTokens),
                item.LeaseExpiresAtUtc, "Task leased to worker.");
            _claimRequests[claimKey] = new ClaimRecord(request.RequestId, response);
            return response;
        }
    }

    public TaskStatusUpdateResponse UpdateTaskStatus(TaskStatusUpdateRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RequestId))
        {
            return new TaskStatusUpdateResponse(false, TaskWorkStatus.Failed, "Request ID is required.",
                new ProtocolError("invalid_status_update", "Request ID is required."));
        }

        lock (_gate)
        {
            if (!TryGetOwnedLease(request.TaskId, request.WorkerId, request.LeaseId, out var item))
            {
                return new TaskStatusUpdateResponse(false, TaskWorkStatus.Failed, "Task lease is invalid or expired.",
                    new ProtocolError("invalid_lease", "Task lease is invalid or expired."));
            }

            if (request.Status != TaskWorkStatus.Running || item.Status != TaskWorkStatus.Leased)
            {
                return new TaskStatusUpdateResponse(false, item.Status, "Only a leased task can transition to running.",
                    new ProtocolError("invalid_transition", "Only a leased task can transition to running."));
            }

            item.Status = TaskWorkStatus.Running;
            return new TaskStatusUpdateResponse(true, item.Status, "Task is running.");
        }
    }

    public TaskResultResponse SubmitResult(TaskResultRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
            string.IsNullOrWhiteSpace(request.TaskId) || string.IsNullOrWhiteSpace(request.WorkerId) ||
            string.IsNullOrWhiteSpace(request.LeaseId) || request.Attempt <= 0 || !Enum.IsDefined(request.Status))
        {
            return ResultError("invalid_result", "Required result fields are invalid.");
        }

        lock (_gate)
        {
            if (_resultRequests.TryGetValue(request.IdempotencyKey, out var previous))
            {
                return previous.Request == request
                    ? previous.Response
                    : ResultError("idempotency_conflict", "Idempotency key was already used for a different result.");
            }

            if (!TryGetOwnedLease(request.TaskId, request.WorkerId, request.LeaseId, out var item) || item.Attempt != request.Attempt)
            {
                return ResultError("invalid_lease", "Task lease, worker, or attempt does not match or has expired.");
            }

            if (item.Status is not (TaskWorkStatus.Leased or TaskWorkStatus.Running))
            {
                return ResultError("invalid_transition", "Task is not accepting a result.");
            }

            item.Status = request.Status == TaskResultStatus.Succeeded ? TaskWorkStatus.Succeeded : TaskWorkStatus.Failed;
            item.Output = request.Output;
            item.ErrorCode = request.ErrorCode;
            var accepted = new TaskResultResponse(true, "Task result accepted.");
            _resultRequests[request.IdempotencyKey] = new ResultRecord(request, accepted);
            return accepted;
        }
    }

    public int RequeueExpiredLeases(DateTimeOffset nowUtc)
    {
        lock (_gate)
        {
            var changed = 0;
            foreach (var item in _tasks.Values.Where(x => x.Status is TaskWorkStatus.Leased or TaskWorkStatus.Running)
                         .Where(x => x.LeaseExpiresAtUtc <= nowUtc))
            {
                item.WorkerId = null;
                item.LeaseId = null;
                item.LeaseExpiresAtUtc = DateTimeOffset.MinValue;
                item.Status = item.Attempt >= item.Request.MaxAttempts ? TaskWorkStatus.Failed : TaskWorkStatus.Queued;
                changed++;
            }

            foreach (var worker in _workers.Values)
            {
                if (worker.LastHeartbeatUtc is not null && nowUtc - worker.LastHeartbeatUtc > HeartbeatTimeout)
                {
                    worker.LastStatus = WorkerRunStatus.Unhealthy;
                }
            }

            return changed;
        }
    }

    public TaskWorkStatus? GetTaskStatus(string taskId)
    {
        lock (_gate)
        {
            return _tasks.TryGetValue(taskId, out var task) ? task.Status : null;
        }
    }

    public WorkerRunStatus? GetWorkerStatus(string workerId)
    {
        lock (_gate)
        {
            if (!_workers.TryGetValue(workerId, out var worker))
            {
                return null;
            }

            if (worker.LastHeartbeatUtc is null || DateTimeOffset.UtcNow - worker.LastHeartbeatUtc > HeartbeatTimeout)
            {
                return WorkerRunStatus.Unhealthy;
            }

            return worker.LastStatus;
        }
    }

    private bool TryGetOwnedLease(string taskId, string workerId, string leaseId, out WorkItem item)
    {
        if (_tasks.TryGetValue(taskId, out var found) && found.WorkerId == workerId && found.LeaseId == leaseId &&
            found.LeaseExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            item = found;
            return true;
        }

        item = null!;
        return false;
    }

    private static WorkerRegistrationResponse RegistrationResponse(PendingWorker registration) =>
        new(registration.WorkerId, registration.Status, registration.Message);

    private static WorkerHeartbeatResponse RejectedHeartbeat(string code, string message) =>
        new(false, message, HeartbeatInterval, new ProtocolError(code, message));

    private static TaskClaimResponse ClaimError(string code, string message) =>
        new(false, null, null, message, new ProtocolError(code, message));

    private static TaskResultResponse ResultError(string code, string message) =>
        new(false, message, new ProtocolError(code, message));

    private static bool IsValid(WorkerRegistrationRequest request) =>
        request is not null && !string.IsNullOrWhiteSpace(request.WorkerName) && request.WorkerName.Length <= 128 &&
        !string.IsNullOrWhiteSpace(request.AgentVersion) && request.AgentVersion.Length <= 64 &&
        !string.IsNullOrWhiteSpace(request.Hostname) && request.Hostname.Length <= 255 &&
        !string.IsNullOrWhiteSpace(request.RequestId) && request.RequestId.Length <= 128 &&
        request.SupportedBackends is { Count: > 0 and <= 16 } && request.SupportedBackends.All(x => !string.IsNullOrWhiteSpace(x) && x.Length <= 64) &&
        request.Capabilities is not null && IsValid(request.Capabilities);

    private static bool IsValid(WorkerCapabilitiesDto capabilities) =>
        capabilities.CpuThreads is >= 1 and <= 1024 && capabilities.AvailableMemoryBytes >= 0 &&
        capabilities.Gpus is { Count: <= 16 } && capabilities.Gpus.All(gpu => gpu is not null &&
            !string.IsNullOrWhiteSpace(gpu.Name) && gpu.Name.Length <= 128 && gpu.TotalMemoryBytes >= 0 &&
            gpu.AvailableMemoryBytes >= 0 && gpu.AvailableMemoryBytes <= gpu.TotalMemoryBytes &&
            !string.IsNullOrWhiteSpace(gpu.Backend) && gpu.Backend.Length <= 64);

    private sealed class PendingWorker(string workerId, WorkerRegistrationRequest request)
    {
        public string WorkerId { get; } = workerId;
        public WorkerRegistrationRequest Request { get; } = request;
        public WorkerRegistrationStatus Status { get; set; } = WorkerRegistrationStatus.PendingApproval;
        public string Message { get; set; } = "Registration is waiting for coordinator approval.";
    }

    private sealed record ClaimRecord(string RequestId, TaskClaimResponse Response);
    private sealed record ResultRecord(TaskResultRequest Request, TaskResultResponse Response);

    private sealed class WorkerState(WorkerRegistrationRequest request)
    {
        public WorkerRegistrationRequest Registration { get; } = request;
        public long LastSequenceNumber { get; set; }
        public DateTimeOffset? LastHeartbeatUtc { get; set; }
        public WorkerRunStatus LastStatus { get; set; } = WorkerRunStatus.Unhealthy;
        public WorkerCapabilitiesDto Capacity { get; set; } = request.Capabilities;
    }

    private sealed class WorkItem(EnqueueTaskRequest request)
    {
        public EnqueueTaskRequest Request { get; } = request;
        public DateTimeOffset CreatedUtc { get; } = DateTimeOffset.UtcNow;
        public TaskWorkStatus Status { get; set; } = TaskWorkStatus.Queued;
        public string? WorkerId { get; set; }
        public string? LeaseId { get; set; }
        public DateTimeOffset LeaseExpiresAtUtc { get; set; }
        public int Attempt { get; set; }
        public string? Output { get; set; }
        public string? ErrorCode { get; set; }
    }
}
