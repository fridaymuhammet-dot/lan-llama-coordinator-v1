using LanLlamaCoordinator.Core.Models;
using LanLlamaCoordinator.Core.Services;

namespace LanLlamaCoordinator.Core.Tests;

public class DeviceRegistryTests
{
    [Fact]
    public void Register_ShouldStoreDevice()
    {
        var registry = new DeviceRegistryService();
        var device = new DeviceInfo
        {
            DeviceId = "device-1",
            Name = "Laptop 01",
            Type = DeviceType.Laptop,
            Network = NetworkType.Ethernet,
            Status = DeviceStatus.Online,
            Trusted = true
        };

        registry.Register(device);

        Assert.Equal(device, registry.Get("device-1"));
    }

    [Fact]
    public void Score_ShouldPreferEthernetOverWifi()
    {
        var scoring = new CapabilityScoringService();

        var ethernetDevice = new DeviceInfo
        {
            DeviceId = "eth-device",
            Type = DeviceType.Desktop,
            Network = NetworkType.Ethernet,
            Status = DeviceStatus.Online,
            Trusted = true,
            Capabilities = new DeviceCapability { SupportsLlamaCpp = true, RamGb = 32, VramGb = 8 }
        };

        var wifiDevice = new DeviceInfo
        {
            DeviceId = "wifi-device",
            Type = DeviceType.Laptop,
            Network = NetworkType.Wifi,
            Status = DeviceStatus.Online,
            Trusted = true,
            Capabilities = new DeviceCapability { SupportsLlamaCpp = true, RamGb = 16, VramGb = 0 }
        };

        var task = new TaskRequest { ModelName = "qwen2.5-coder-7b", RequiresGpu = false, RequiredRamMb = 8192 };

        Assert.True(scoring.Score(ethernetDevice, task) > scoring.Score(wifiDevice, task));
    }
}
