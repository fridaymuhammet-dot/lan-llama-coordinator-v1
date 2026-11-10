using LanLlamaCoordinator.Core.Models;
using LanLlamaCoordinator.Core.Services;

namespace LanLlamaCoordinator.Infrastructure;

public sealed class StubWorkerBroker
{
    private readonly DeviceRegistryService _deviceRegistry;
    private readonly HealthChecker _healthChecker;

    public StubWorkerBroker(DeviceRegistryService deviceRegistry, HealthChecker healthChecker)
    {
        _deviceRegistry = deviceRegistry;
        _healthChecker = healthChecker;
    }

    public bool TryRegister(DeviceInfo device)
    {
        if (device is null)
        {
            return false;
        }

        _deviceRegistry.Register(device);
        return true;
    }

    public bool IsNodeAvailable(DeviceInfo device, WorkerHeartbeat heartbeat, TaskRequest taskRequest)
    {
        return _healthChecker.IsHealthy(device, heartbeat, taskRequest);
    }
}
