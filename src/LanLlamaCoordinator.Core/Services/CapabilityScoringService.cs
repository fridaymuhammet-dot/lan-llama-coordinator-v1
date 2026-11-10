using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Core.Services;

public sealed class CapabilityScoringService
{
    public int Score(DeviceInfo device, TaskRequest taskRequest)
    {
        var score = 0;

        if (device.Status == DeviceStatus.Online)
        {
            score += 50;
        }
        else if (device.Status == DeviceStatus.Busy)
        {
            score += 25;
        }

        score += device.Trusted ? 20 : 0;

        score += device.Network switch
        {
            NetworkType.Ethernet => 30,
            NetworkType.Wifi => 15,
            NetworkType.Usb => 10,
            _ => 0
        };

        if (device.Capabilities.SupportsLlamaCpp && taskRequest.ModelName.Length > 0)
        {
            score += 20;
        }

        if (taskRequest.RequiresGpu && device.Capabilities.VramGb > 0)
        {
            score += 20;
        }

        if (taskRequest.RequiredRamMb <= device.Capabilities.RamGb * 1024)
        {
            score += 10;
        }

        return score;
    }
}
