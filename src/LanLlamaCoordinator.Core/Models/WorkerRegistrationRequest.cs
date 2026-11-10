namespace LanLlamaCoordinator.Core.Models;

public enum NodeRole
{
    Coordinator,
    Worker,
    CoordinatorAndWorker
}

public sealed class WorkerRegistrationRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DeviceType Type { get; set; } = DeviceType.Laptop;
    public string Platform { get; set; } = string.Empty;
    public NetworkType Network { get; set; } = NetworkType.Unknown;
    public DeviceCapability Capabilities { get; set; } = new();
}

public enum RegistrationResult
{
    PendingApproval,
    Approved,
    Rejected,
    InvalidRequest
}
