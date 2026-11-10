using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Core.Services;

public interface ITaskAssignmentService
{
    TaskAssignment? AssignBestDevice(IEnumerable<DeviceInfo> devices, TaskRequest taskRequest);
    TaskAssignment? AssignBestDevice(TaskRequest taskRequest, DeviceInfo? preferredDevice = null);
}

public sealed class TaskAssignmentService : ITaskAssignmentService
{
    private readonly IDeviceRegistry _deviceRegistry;
    private readonly IHealthChecker _healthChecker;
    private readonly CapabilityScoringService _scoringService;

    public TaskAssignmentService(IDeviceRegistry deviceRegistry, IHealthChecker healthChecker, CapabilityScoringService scoringService)
    {
        _deviceRegistry = deviceRegistry;
        _healthChecker = healthChecker;
        _scoringService = scoringService;
    }

    public TaskAssignment? AssignBestDevice(IEnumerable<DeviceInfo> devices, TaskRequest taskRequest)
    {
        ArgumentNullException.ThrowIfNull(devices);
        ArgumentNullException.ThrowIfNull(taskRequest);

        var device = devices
            .Where(d => d.Status == DeviceStatus.Online || d.Status == DeviceStatus.Busy)
            .Where(d => !d.MaintenanceMode)
            .Where(d => d.Trusted)
            .Where(d => _healthChecker.IsHealthy(d, new WorkerHeartbeat { DeviceId = d.DeviceId, Status = d.Status, TimestampUtc = DateTimeOffset.UtcNow }, taskRequest))
            .OrderByDescending(d => _scoringService.Score(d, taskRequest))
            .FirstOrDefault();

        if (device is null)
        {
            return null;
        }

        return new TaskAssignment
        {
            TaskId = taskRequest.TaskId,
            DeviceId = device.DeviceId,
            AssignedUtc = DateTimeOffset.UtcNow,
            State = "assigned"
        };
    }

    public TaskAssignment? AssignBestDevice(TaskRequest taskRequest, DeviceInfo? preferredDevice = null)
    {
        var candidates = _deviceRegistry.GetAll();

        if (preferredDevice is not null)
        {
            candidates = candidates.Where(d => d.DeviceId == preferredDevice.DeviceId).ToList();
        }

        return AssignBestDevice(candidates, taskRequest);
    }
}
