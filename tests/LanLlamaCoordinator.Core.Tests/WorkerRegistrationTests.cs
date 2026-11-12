using LanLlamaCoordinator.Core.Models;
using LanLlamaCoordinator.Core.Services;

namespace LanLlamaCoordinator.Core.Tests;

public class WorkerRegistrationTests
{
    [Fact]
    public void Registration_RequiresOperatorApprovalAndPairingTokenBeforeHeartbeatIsAccepted()
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
        Assert.False(service.ReceiveHeartbeat(CreateHealthyHeartbeat("worker-1"), null));
        Assert.Null(service.IssuePairingToken("worker-1"));

        var approved = service.Approve("worker-1");
        Assert.NotNull(approved);
        Assert.True(approved!.Trusted);
        Assert.Equal(DeviceStatus.Offline, approved.Status);
        Assert.False(service.ReceiveHeartbeat(CreateHealthyHeartbeat("worker-1"), null));

        var pairingToken = service.IssuePairingToken("worker-1");
        Assert.False(string.IsNullOrWhiteSpace(pairingToken));
        Assert.True(service.ReceiveHeartbeat(CreateHealthyHeartbeat("worker-1"), pairingToken));
        Assert.Equal(DeviceStatus.Online, approved.Status);
    }

    [Fact]
    public void HeartbeatRejectsWrongAndRevokedPairingTokens()
    {
        var registry = new DeviceRegistryService();
        var service = new WorkerRegistrationService(registry, new HealthChecker());
        service.Submit(new WorkerRegistrationRequest
        {
            DeviceId = "worker-auth",
            Name = "Worker",
            Platform = "windows",
            Capabilities = new DeviceCapability { RamGb = 16 }
        });
        service.Approve("worker-auth");
        var token = service.IssuePairingToken("worker-auth");

        Assert.False(service.ReceiveHeartbeat(CreateHealthyHeartbeat("worker-auth"), "wrong-token"));
        Assert.True(service.ReceiveHeartbeat(CreateHealthyHeartbeat("worker-auth"), token));
        Assert.True(service.RevokePairingToken("worker-auth"));
        Assert.False(service.ReceiveHeartbeat(CreateHealthyHeartbeat("worker-auth"), token));
    }

    [Fact]
    public void HealthyHeartbeatCanRecoverAnUnhealthyWorker()
    {
        var registry = new DeviceRegistryService();
        var service = new WorkerRegistrationService(registry, new HealthChecker());
        service.Submit(new WorkerRegistrationRequest
        {
            DeviceId = "worker-recovery",
            Name = "Worker",
            Platform = "windows",
            Capabilities = new DeviceCapability { RamGb = 16 }
        });
        var worker = service.Approve("worker-recovery");
        Assert.NotNull(worker);
        var pairingToken = service.IssuePairingToken("worker-recovery");

        var unhealthyHeartbeat = CreateHealthyHeartbeat("worker-recovery");
        unhealthyHeartbeat.CpuPercent = 99;
        Assert.False(service.ReceiveHeartbeat(unhealthyHeartbeat, pairingToken));
        Assert.Equal(DeviceStatus.Unhealthy, worker!.Status);

        Assert.True(service.ReceiveHeartbeat(CreateHealthyHeartbeat("worker-recovery"), pairingToken));
        Assert.Equal(DeviceStatus.Online, worker.Status);
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
        var pairingToken = service.IssuePairingToken("worker-2");

        var accepted = service.ReceiveHeartbeat(new WorkerHeartbeat
        {
            DeviceId = "worker-2",
            TimestampUtc = DateTimeOffset.UtcNow,
            Status = DeviceStatus.Online,
            CpuPercent = 101,
            RamPercent = 0,
            GpuPercent = 0,
            TemperatureC = 40
        }, pairingToken);

        Assert.False(accepted);
    }

    private static WorkerHeartbeat CreateHealthyHeartbeat(string workerId) => new()
    {
        DeviceId = workerId,
        TimestampUtc = DateTimeOffset.UtcNow,
        Status = DeviceStatus.Online,
        CpuPercent = 10,
        RamPercent = 30,
        GpuPercent = 0,
        TemperatureC = 45
    };
}
