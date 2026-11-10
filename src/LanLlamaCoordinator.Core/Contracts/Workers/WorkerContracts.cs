using LanLlamaCoordinator.Core.Contracts.Common;

namespace LanLlamaCoordinator.Core.Contracts.Workers;

public sealed record WorkerRegistrationRequest(
    string WorkerName,
    string AgentVersion,
    string Hostname,
    IReadOnlyList<string> SupportedBackends,
    WorkerCapabilitiesDto Capabilities,
    string RequestId);

public sealed record WorkerCapabilitiesDto(
    int CpuThreads,
    long AvailableMemoryBytes,
    IReadOnlyList<WorkerGpuDto> Gpus);

public sealed record WorkerGpuDto(
    string Name,
    long TotalMemoryBytes,
    long AvailableMemoryBytes,
    string Backend);

public enum WorkerRegistrationStatus
{
    PendingApproval,
    Approved,
    Rejected,
    InvalidRequest
}

public sealed record WorkerRegistrationResponse(
    string WorkerId,
    WorkerRegistrationStatus Status,
    string Message,
    ProtocolError? Error = null);

public sealed record WorkerApprovalRequest(
    string RequestId,
    bool Approve,
    string? Reason = null);

public sealed record WorkerApprovalResponse(
    string WorkerId,
    WorkerRegistrationStatus Status,
    string Message);

public enum WorkerRunStatus
{
    Idle,
    Busy,
    Unhealthy
}

public sealed record WorkerHeartbeatRequest(
    string WorkerId,
    long SequenceNumber,
    DateTimeOffset SentAtUtc,
    WorkerRunStatus Status,
    WorkerCapabilitiesDto CurrentCapacity,
    string RequestId);

public sealed record WorkerHeartbeatResponse(
    bool Accepted,
    string Message,
    TimeSpan RecommendedInterval,
    ProtocolError? Error = null);
