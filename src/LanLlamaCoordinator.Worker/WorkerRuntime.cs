using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Worker;

public sealed class WorkerRuntime
{
    public string DeviceId { get; }
    public WorkerHeartbeat LastHeartbeat { get; private set; }

    public WorkerRuntime(string deviceId)
    {
        DeviceId = deviceId;
        LastHeartbeat = new WorkerHeartbeat { DeviceId = deviceId, Status = DeviceStatus.Online };
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
            TemperatureC = temperatureC,
            Models = new List<ModelInfo>
            {
                new() { Name = "default-model", Format = "gguf", Loaded = false, MemoryMb = 2048, Compatible = true }
            }
        };

        return LastHeartbeat;
    }
}
