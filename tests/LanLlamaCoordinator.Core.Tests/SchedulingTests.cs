using LanLlamaCoordinator.Core.Models;
using LanLlamaCoordinator.Core.Services;

namespace LanLlamaCoordinator.Core.Tests;

public class SchedulingTests
{
    [Fact]
    public void LifecycleManager_ShouldMarkHeartbeatExpiredDeviceUnhealthy()
    {
        var registry = new DeviceRegistryService();
        var healthChecker = new HealthChecker();
        var lifecycle = new DeviceLifecycleManager(registry, healthChecker);

        var device = new DeviceInfo
        {
            DeviceId = "device-1",
            Name = "Laptop 01",
            Type = DeviceType.Laptop,
            Network = NetworkType.Ethernet,
            Status = DeviceStatus.Online,
            Trusted = true,
            Capabilities = new DeviceCapability { RamGb = 32, SupportsLlamaCpp = true }
        };

        lifecycle.RegisterOrUpdate(device);

        lifecycle.UpdateHeartbeat(new WorkerHeartbeat
        {
            DeviceId = "device-1",
            Status = DeviceStatus.Online,
            TimestampUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            CpuPercent = 10,
            RamPercent = 20,
            GpuPercent = 0,
            TemperatureC = 45
        });

        var updated = lifecycle.Get("device-1");
        Assert.NotNull(updated);
        Assert.Equal(DeviceStatus.Unhealthy, updated!.Status);
    }

    [Fact]
    public void TaskAssignmentService_ShouldPreferEthernetDevice()
    {
        var registry = new DeviceRegistryService();
        var health = new HealthChecker();
        var scoring = new CapabilityScoringService();
        var service = new TaskAssignmentService(registry, health, scoring);

        var ethernet = new DeviceInfo
        {
            DeviceId = "ethernet-device",
            Name = "Ethernet Node",
            Type = DeviceType.Desktop,
            Network = NetworkType.Ethernet,
            Status = DeviceStatus.Online,
            Trusted = true,
            Capabilities = new DeviceCapability { RamGb = 64, VramGb = 12, SupportsLlamaCpp = true }
        };

        var wifi = new DeviceInfo
        {
            DeviceId = "wifi-device",
            Name = "WiFi Node",
            Type = DeviceType.Laptop,
            Network = NetworkType.Wifi,
            Status = DeviceStatus.Online,
            Trusted = true,
            Capabilities = new DeviceCapability { RamGb = 32, VramGb = 0, SupportsLlamaCpp = true }
        };

        registry.Register(ethernet);
        registry.Register(wifi);

        var task = new TaskRequest
        {
            TaskId = "task-1",
            ModelName = "qwen2.5-coder-7b",
            RequiresGpu = false,
            RequiredRamMb = 16384
        };

        var assignment = service.AssignBestDevice(task);

        Assert.NotNull(assignment);
        Assert.Equal("ethernet-device", assignment!.DeviceId);
    }
}
