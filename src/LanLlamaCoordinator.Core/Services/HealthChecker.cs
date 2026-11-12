using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Core.Services;

public interface IHealthChecker
{
    bool IsHealthy(DeviceInfo device, TaskRequest? taskRequest = null);
    bool IsHealthy(DeviceInfo device, WorkerHeartbeat heartbeat, TaskRequest? taskRequest = null);
}

public sealed class HealthChecker : IHealthChecker
{
    public bool IsHealthy(DeviceInfo device, TaskRequest? taskRequest = null)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (device.MaintenanceMode)
        {
            return false;
        }

        if (device.Status is DeviceStatus.Unhealthy or DeviceStatus.Offline)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        if (now - device.LastSeenUtc > TimeSpan.FromSeconds(25))
        {
            return false;
        }

        if (taskRequest is not null && taskRequest.RequiredRamMb > device.Capabilities.RamGb * 1024)
        {
            return false;
        }

        return true;
    }

    public bool IsHealthy(DeviceInfo device, WorkerHeartbeat heartbeat, TaskRequest? taskRequest = null)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(heartbeat);

        if (device.MaintenanceMode)
        {
            return false;
        }

        if (heartbeat.Status is DeviceStatus.Unhealthy or DeviceStatus.Offline)
        {
            return false;
        }

        if (heartbeat.CpuPercent > 95 || heartbeat.RamPercent > 95)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        if (now - heartbeat.TimestampUtc > TimeSpan.FromSeconds(25))
        {
            return false;
        }

        if (taskRequest is not null && taskRequest.RequiredRamMb > device.Capabilities.RamGb * 1024)
        {
            return false;
        }

        device.LastSeenUtc = heartbeat.TimestampUtc;
        device.Status = heartbeat.Status;

        return true;
    }
}
