using LanLlamaCoordinator.Core.Models;
using LanLlamaCoordinator.Core.Services;

namespace LanLlamaCoordinator.Core.Tests;

public class WorkerRegistrationTests
{
    [Fact]
    public void Registration_RequiresOperatorApprovalBeforeHeartbeatIsAccepted()
    {
        var registry = new DeviceRegistryService();
        var service = new WorkerRegistrationService(registry, new HealthChecker());
        var request = new WorkerRegistrationRequest
        {
            DeviceId = "worker-1",
            Name = "Laptop worker",
            Type = DeviceType.Laptop,
            Platform = "windows",
            Network = NetworkType.Ethernet,
            Capabilities = new DeviceCapability { RamGb = 16, SupportsLlamaCpp = true }
        };

        Assert.Equal(RegistrationResult.PendingApproval, service.Submit(request));
        Assert.Null(registry.Get("worker-1"));
        Assert.False(service.ReceiveHeartbeat(new WorkerHeartbeat { DeviceId = "worker-1" }));

        var approved = service.Approve("worker-1");
        Assert.NotNull(approved);
        Assert.True(approved!.Trusted);
        Assert.Equal(DeviceStatus.Offline, approved.Status);

        var accepted = service.ReceiveHeartbeat(new WorkerHeartbeat
        {
            DeviceId = "worker-1",
            TimestampUtc = DateTimeOffset.UtcNow,
            Status = DeviceStatus.Online,
            CpuPercent = 10,
            RamPercent = 30,
            GpuPercent = 0,
            TemperatureC = 45
        });

        Assert.True(accepted);
        Assert.Equal(DeviceStatus.Online, approved.Status);
    }

    [Fact]
    public void Registration_RejectsInvalidHeartbeatMetrics()
    {
        var registry = new DeviceRegistryService();
        var service = new WorkerRegistrationService(registry, new HealthChecker());
        service.Submit(new WorkerRegistrationRequest
        {
            DeviceId = "worker-2",
            Name = "Worker",
            Platform = "windows"
        });
        service.Approve("worker-2");

        var accepted = service.ReceiveHeartbeat(new WorkerHeartbeat
        {
            DeviceId = "worker-2",
            TimestampUtc = DateTimeOffset.UtcNow,
            Status = DeviceStatus.Online,
            CpuPercent = 101,
            RamPercent = 0,
            GpuPercent = 0,
            TemperatureC = 40
        });

        Assert.False(accepted);
    }
}
