using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Core.Services;

/// <summary>
/// In-memory pairing boundary for the prototype. Network transport and durable identity storage are not implemented here.
/// A worker is never trusted merely because it can reach the coordinator: an operator must approve it first.
/// </summary>
public sealed class WorkerRegistrationService
{
    private readonly IDeviceRegistry _registry;
    private readonly IHealthChecker _healthChecker;
    private readonly Dictionary<string, WorkerRegistrationRequest> _pending = new(StringComparer.OrdinalIgnoreCase);

    public WorkerRegistrationService(IDeviceRegistry registry, IHealthChecker healthChecker)
    {
        _registry = registry;
        _healthChecker = healthChecker;
    }

    public IReadOnlyCollection<WorkerRegistrationRequest> PendingApprovals => _pending.Values.ToList();

    public RegistrationResult Submit(WorkerRegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.DeviceId) || request.DeviceId.Length > 128 ||
            string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 128 ||
            string.IsNullOrWhiteSpace(request.Platform) || request.Platform.Length > 64)
        {
            return RegistrationResult.InvalidRequest;
        }

        if (_registry.Get(request.DeviceId) is not null)
        {
            return RegistrationResult.Approved;
        }

        _pending[request.DeviceId] = request;
        return RegistrationResult.PendingApproval;
    }

    public DeviceInfo? Approve(string deviceId)
    {
        if (!_pending.Remove(deviceId, out var request))
        {
            return null;
        }

        var device = new DeviceInfo
        {
            DeviceId = request.DeviceId,
            Name = request.Name,
            Type = request.Type,
            Platform = request.Platform,
            Network = request.Network,
            Status = DeviceStatus.Offline,
            Capabilities = request.Capabilities,
            Trusted = true,
            LastSeenUtc = DateTimeOffset.MinValue
        };

        _registry.Register(device);
        return device;
    }

    public bool Reject(string deviceId) => _pending.Remove(deviceId);

    public bool ReceiveHeartbeat(WorkerHeartbeat heartbeat)
    {
        ArgumentNullException.ThrowIfNull(heartbeat);
        var device = _registry.Get(heartbeat.DeviceId);
        if (device is null || !device.Trusted || !IsValidHeartbeat(heartbeat))
        {
            return false;
        }

        if (heartbeat.Status is not (DeviceStatus.Online or DeviceStatus.Busy))
        {
            return false;
        }

        device.LastSeenUtc = heartbeat.TimestampUtc;
        device.Status = _healthChecker.IsHealthy(device, heartbeat)
            ? heartbeat.Status
            : DeviceStatus.Unhealthy;

        return device.Status is DeviceStatus.Online or DeviceStatus.Busy;
    }

    private static bool IsValidHeartbeat(WorkerHeartbeat heartbeat)
    {
        var now = DateTimeOffset.UtcNow;
        return !string.IsNullOrWhiteSpace(heartbeat.DeviceId)
            && heartbeat.TimestampUtc <= now.AddSeconds(30)
            && heartbeat.CpuPercent is >= 0 and <= 100
            && heartbeat.RamPercent is >= 0 and <= 100
            && heartbeat.GpuPercent is >= 0 and <= 100
            && heartbeat.TemperatureC is >= 0 and <= 150;
    }
}
