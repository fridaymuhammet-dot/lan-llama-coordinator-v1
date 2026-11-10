using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Core.Services;

public interface IDeviceLifecycleManager
{
    void RegisterOrUpdate(DeviceInfo device);
    void UpdateHeartbeat(WorkerHeartbeat heartbeat);
    void Remove(string deviceId);
    IReadOnlyCollection<DeviceInfo> GetDevices();
    DeviceInfo? Get(string deviceId);
    DeviceInfo? GetHealthyCandidate(TaskRequest taskRequest);
}

public sealed class DeviceLifecycleManager : IDeviceLifecycleManager
{
    private readonly IDeviceRegistry _deviceRegistry;
    private readonly IHealthChecker _healthChecker;
    private readonly Dictionary<string, WorkerHeartbeat> _heartbeats = new(StringComparer.OrdinalIgnoreCase);

    public DeviceLifecycleManager(IDeviceRegistry deviceRegistry, IHealthChecker healthChecker)
    {
        _deviceRegistry = deviceRegistry;
        _healthChecker = healthChecker;
    }

    public void RegisterOrUpdate(DeviceInfo device)
    {
        ArgumentNullException.ThrowIfNull(device);
        _deviceRegistry.Register(device);

        var existing = _deviceRegistry.Get(device.DeviceId);
        if (existing is not null)
        {
            existing.Status = device.Status;
            existing.Trusted = device.Trusted;
            existing.Network = device.Network;
            existing.Capabilities = device.Capabilities;
            existing.MaintenanceMode = device.MaintenanceMode;
        }
    }

    public void UpdateHeartbeat(WorkerHeartbeat heartbeat)
    {
        ArgumentNullException.ThrowIfNull(heartbeat);

        _heartbeats[heartbeat.DeviceId] = heartbeat;

        var device = _deviceRegistry.Get(heartbeat.DeviceId);
        if (device is null)
        {
            return;
        }

        device.LastSeenUtc = heartbeat.TimestampUtc;
        device.Status = _healthChecker.IsHealthy(device, heartbeat)
            ? (heartbeat.Status == DeviceStatus.Busy ? DeviceStatus.Busy : DeviceStatus.Online)
            : DeviceStatus.Unhealthy;
    }

    public void Remove(string deviceId)
    {
        _deviceRegistry.Remove(deviceId);
        _heartbeats.Remove(deviceId);
    }

    public IReadOnlyCollection<DeviceInfo> GetDevices() => _deviceRegistry.GetAll();

    public DeviceInfo? Get(string deviceId) => _deviceRegistry.Get(deviceId);

    public DeviceInfo? GetHealthyCandidate(TaskRequest taskRequest)
    {
        var candidates = _deviceRegistry.GetAvailable()
            .Where(device => _heartbeats.TryGetValue(device.DeviceId, out var heartbeat) && _healthChecker.IsHealthy(device, heartbeat, taskRequest));

        return candidates
            .OrderByDescending(device => GetPriorityScore(device, taskRequest))
            .FirstOrDefault();
    }

    private static int GetPriorityScore(DeviceInfo device, TaskRequest taskRequest)
    {
        var score = 0;

        score += device.Network switch
        {
            NetworkType.Ethernet => 50,
            NetworkType.Wifi => 30,
            NetworkType.Usb => 15,
            _ => 0
        };

        score += device.Trusted ? 20 : 0;

        if (taskRequest.RequiresGpu && device.Capabilities.VramGb > 0)
        {
            score += 30;
        }

        if (taskRequest.RequiredRamMb <= device.Capabilities.RamGb * 1024)
        {
            score += 15;
        }

        return score;
    }
}
