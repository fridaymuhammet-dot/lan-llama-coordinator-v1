using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Core.Services;

public interface IHealthChecker
{
    bool IsHealthy(DeviceInfo device, WorkerHeartbeat heartbeat, TaskRequest? taskRequest = null);
}

public sealed class HealthChecker : IHealthChecker
{
    public bool IsHealthy(DeviceInfo device, WorkerHeartbeat heartbeat, TaskRequest? taskRequest = null)
    {
        if (device.MaintenanceMode)
        {
            return false;
        }

        if (device.Status == DeviceStatus.Offline || device.Status == DeviceStatus.Unhealthy)
        {
            return false;
        }

        if (heartbeat.Status == DeviceStatus.Unhealthy || heartbeat.Status == DeviceStatus.Offline)
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

        return true;
    }
}
