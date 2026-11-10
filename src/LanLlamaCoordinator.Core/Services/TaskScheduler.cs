using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Core.Services;

public sealed class TaskScheduler
{
    private readonly IHealthChecker _healthChecker;
    private readonly CapabilityScoringService _scoringService;

    public TaskScheduler(IHealthChecker healthChecker, CapabilityScoringService scoringService)
    {
        _healthChecker = healthChecker;
        _scoringService = scoringService;
    }

    public DeviceInfo? SelectBestCandidate(IEnumerable<DeviceInfo> devices, WorkerHeartbeat heartbeat, TaskRequest taskRequest)
    {
        ArgumentNullException.ThrowIfNull(devices);
        ArgumentNullException.ThrowIfNull(taskRequest);

        return devices
            .Where(device => string.Equals(device.DeviceId, heartbeat.DeviceId, StringComparison.OrdinalIgnoreCase))
            .Where(device => _healthChecker.IsHealthy(device, heartbeat, taskRequest))
            .OrderByDescending(device => _scoringService.Score(device, taskRequest))
            .FirstOrDefault();
    }

    public DeviceInfo? SelectBestCandidate(IEnumerable<DeviceInfo> devices, TaskRequest taskRequest)
    {
        ArgumentNullException.ThrowIfNull(devices);
        ArgumentNullException.ThrowIfNull(taskRequest);

        return devices
            .Where(device => device.Status == DeviceStatus.Online || device.Status == DeviceStatus.Busy)
            .Where(device => !device.MaintenanceMode)
            .Where(device => device.Trusted)
            .OrderByDescending(device => _scoringService.Score(device, taskRequest))
            .FirstOrDefault();
    }
}
