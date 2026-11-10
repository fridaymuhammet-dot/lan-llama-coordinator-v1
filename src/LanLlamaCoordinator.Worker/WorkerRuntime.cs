using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Worker;

public sealed class WorkerRuntime
{
    public string DeviceId { get; }
    public WorkerHeartbeat LastHeartbeat { get; private set; }

    public WorkerRuntime(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("A stable worker device ID is required.", nameof(deviceId));
        }

        DeviceId = deviceId;
        LastHeartbeat = new WorkerHeartbeat { DeviceId = deviceId, Status = DeviceStatus.Online };
    }

    public WorkerRegistrationRequest CreateRegistrationRequest(
        string name,
        DeviceType type,
        string platform,
        NetworkType network,
        DeviceCapability capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        return new WorkerRegistrationRequest
        {
            DeviceId = DeviceId,
            Name = name,
            Type = type,
            Platform = platform,
            Network = network,
            Capabilities = capabilities
        };
    }

    public WorkerHeartbeat ReportStatus(int cpuPercent, int ramPercent, int gpuPercent, int temperatureC)
    {
        LastHeartbeat = new WorkerHeartbeat
        {
            DeviceId = DeviceId,
            TimestampUtc = DateTimeOffset.UtcNow,
            Status = DeviceStatus.Online,
            CpuPercent = cpuPercent,
            RamPercent = ramPercent,
            GpuPercent = gpuPercent,
            TemperatureC = temperatureC
        };

        return LastHeartbeat;
    }
}
