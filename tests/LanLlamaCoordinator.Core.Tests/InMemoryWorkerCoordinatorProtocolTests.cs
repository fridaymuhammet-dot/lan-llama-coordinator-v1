using LanLlamaCoordinator.Core.Contracts.Tasks;
using LanLlamaCoordinator.Core.Contracts.Workers;
using LanLlamaCoordinator.Infrastructure.WorkerProtocol;

namespace LanLlamaCoordinator.Core.Tests;

public class InMemoryWorkerCoordinatorProtocolTests
{
    [Fact]
    public void WorkerMustBeApprovedBeforeHeartbeatAndClaim()
    {
        var protocol = new InMemoryWorkerCoordinatorProtocol();
        var registration = protocol.Register(CreateRegistration("request-1"));

        Assert.Equal(WorkerRegistrationStatus.PendingApproval, registration.Status);
        Assert.False(protocol.Heartbeat(CreateHeartbeat(registration.WorkerId, 1, "hb-1")).Accepted);
        Assert.True(protocol.Enqueue(CreateTask("task-1")));
        Assert.False(protocol.ClaimTask(new TaskClaimRequest(registration.WorkerId, new[] { "llama.cpp" }, 1024, "claim-1")).HasTask);
    }

    [Fact]
    public void ApprovedWorkerCanHeartbeatClaimAndSubmitResultIdempotently()
    {
        var protocol = new InMemoryWorkerCoordinatorProtocol();
        var registration = protocol.Register(CreateRegistration("request-1"));
        var approval = protocol.Approve(new WorkerApprovalRequest("request-1", true));

        Assert.Equal(registration.WorkerId, approval.WorkerId);
        Assert.True(protocol.Heartbeat(CreateHeartbeat(approval.WorkerId, 1, "hb-1")).Accepted);
        Assert.True(protocol.Enqueue(CreateTask("task-1")));

        var claim = protocol.ClaimTask(new TaskClaimRequest(approval.WorkerId, new[] { "llama.cpp" }, 4096, "claim-1"));
        Assert.True(claim.HasTask);
        Assert.NotNull(claim.Task);
        Assert.Equal(TaskWorkStatus.Leased, protocol.GetTaskStatus("task-1"));

        var result = new TaskResultRequest("task-1", approval.WorkerId, claim.Task!.LeaseId, claim.Task.Attempt,
            TaskResultStatus.Succeeded, "ok", null, "result-key-1");
        Assert.True(protocol.SubmitResult(result).Accepted);
        Assert.True(protocol.SubmitResult(result).Accepted);
        Assert.Equal(TaskWorkStatus.Succeeded, protocol.GetTaskStatus("task-1"));
    }

    [Fact]
    public void DuplicateOrOldHeartbeatSequenceIsRejected()
    {
        var protocol = ApprovedProtocol(out var workerId);
        Assert.True(protocol.Heartbeat(CreateHeartbeat(workerId, 2, "hb-2")).Accepted);
        var replay = protocol.Heartbeat(CreateHeartbeat(workerId, 2, "hb-replay"));
        Assert.False(replay.Accepted);
        Assert.Equal("stale_sequence", replay.Error?.Code);
    }

    [Fact]
    public void IncompatibleWorkerDoesNotReceiveTaskAndWrongWorkerCannotSubmitResult()
    {
        var protocol = ApprovedProtocol(out var workerId);
        Assert.True(protocol.Heartbeat(CreateHeartbeat(workerId, 1, "hb-1")).Accepted);
        Assert.True(protocol.Enqueue(CreateTask("task-1")));

        var noCompatibleWork = protocol.ClaimTask(new TaskClaimRequest(workerId, new[] { "other-backend" }, 4096, "claim-no-match"));
        Assert.False(noCompatibleWork.HasTask);

        var claim = protocol.ClaimTask(new TaskClaimRequest(workerId, new[] { "llama.cpp" }, 4096, "claim-match"));
        Assert.True(claim.HasTask);
        var forged = new TaskResultRequest("task-1", "another-worker", claim.Task!.LeaseId, claim.Task.Attempt,
            TaskResultStatus.Succeeded, "bad", null, "forged-result");
        Assert.False(protocol.SubmitResult(forged).Accepted);
    }

    [Fact]
    public void ExpiredLeaseReturnsTaskToQueueUntilAttemptLimit()
    {
        var protocol = ApprovedProtocol(out var workerId);
        Assert.True(protocol.Heartbeat(CreateHeartbeat(workerId, 1, "hb-1")).Accepted);
        Assert.True(protocol.Enqueue(CreateTask("task-1", maxAttempts: 1)));
        var claim = protocol.ClaimTask(new TaskClaimRequest(workerId, new[] { "llama.cpp" }, 4096, "claim-1"));
        Assert.True(claim.HasTask);

        protocol.RequeueExpiredLeases(DateTimeOffset.UtcNow.AddMinutes(2));

        Assert.Equal(TaskWorkStatus.Failed, protocol.GetTaskStatus("task-1"));
    }

    private static InMemoryWorkerCoordinatorProtocol ApprovedProtocol(out string workerId)
    {
        var protocol = new InMemoryWorkerCoordinatorProtocol();
        var registration = protocol.Register(CreateRegistration("request-1"));
        protocol.Approve(new WorkerApprovalRequest("request-1", true));
        workerId = registration.WorkerId;
        return protocol;
    }

    private static WorkerRegistrationRequest CreateRegistration(string requestId) => new(
        "worker", "0.1.0", "host", new[] { "llama.cpp" },
        new WorkerCapabilitiesDto(8, 8_000_000_000, Array.Empty<WorkerGpuDto>()), requestId);

    private static WorkerHeartbeatRequest CreateHeartbeat(string workerId, long sequence, string requestId) => new(
        workerId, sequence, DateTimeOffset.UtcNow, WorkerRunStatus.Idle,
        new WorkerCapabilitiesDto(8, 8_000_000_000, Array.Empty<WorkerGpuDto>()), requestId);

    private static EnqueueTaskRequest CreateTask(string taskId, int maxAttempts = 3) => new(
        taskId, "test-model", "llama.cpp", 1024, "hello", 32, maxAttempts);
}
