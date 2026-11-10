namespace LanLlamaCoordinator.Core.Contracts.Tasks;

public sealed record TaskClaimRequest(
    string WorkerId,
    IReadOnlyList<string> SupportedBackends,
    long AvailableMemoryBytes,
    string RequestId);

public sealed record TaskClaimResponse(
    bool HasTask,
    ClaimedTaskDto? Task,
    DateTimeOffset? LeaseExpiresAtUtc,
    string Message,
    ProtocolError? Error = null);

public sealed record ClaimedTaskDto(
    string TaskId,
    string LeaseId,
    int Attempt,
    string ModelName,
    string Backend,
    string Prompt,
    int MaxTokens);

public enum TaskResultStatus
{
    Succeeded,
    Failed
}

public sealed record TaskResultRequest(
    string TaskId,
    string WorkerId,
    string LeaseId,
    int Attempt,
    TaskResultStatus Status,
    string? Output,
    string? ErrorCode,
    string IdempotencyKey);

public sealed record TaskResultResponse(
    bool Accepted,
    string Message,
    ProtocolError? Error = null);

public enum TaskWorkStatus
{
    Queued,
    Leased,
    Running,
    Succeeded,
    Failed
}

public sealed record TaskStatusUpdateRequest(
    string TaskId,
    string WorkerId,
    string LeaseId,
    TaskWorkStatus Status,
    string RequestId);

public sealed record TaskStatusUpdateResponse(
    bool Accepted,
    TaskWorkStatus Status,
    string Message,
    ProtocolError? Error = null);

/// <summary>Coordinator-side task input for the in-memory protocol prototype.</summary>
public sealed record EnqueueTaskRequest(
    string TaskId,
    string ModelName,
    string RequiredBackend,
    long RequiredMemoryBytes,
    string Prompt,
    int MaxTokens,
    int MaxAttempts = 3);
