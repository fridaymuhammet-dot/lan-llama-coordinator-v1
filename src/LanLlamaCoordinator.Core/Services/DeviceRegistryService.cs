using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Core.Services;

public interface IDeviceRegistry
{
    void Register(DeviceInfo device);
    void Remove(string deviceId);
    DeviceInfo? Get(string deviceId);
    IReadOnlyCollection<DeviceInfo> GetAll();
    IReadOnlyCollection<DeviceInfo> GetAvailable();
}

public sealed class DeviceRegistryService : IDeviceRegistry
{
    private readonly Dictionary<string, DeviceInfo> _devices = new(StringComparer.OrdinalIgnoreCase);

    public void Register(DeviceInfo device)
    {
        ArgumentNullException.ThrowIfNull(device);
        _devices[device.DeviceId] = device;
    }

    public void Remove(string deviceId)
    {
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            _devices.Remove(deviceId);
        }
    }

    public DeviceInfo? Get(string deviceId)
    {
        return string.IsNullOrWhiteSpace(deviceId) ? null : _devices.TryGetValue(deviceId, out var device) ? device : null;
    }

    public IReadOnlyCollection<DeviceInfo> GetAll() => _devices.Values.ToList();

    public IReadOnlyCollection<DeviceInfo> GetAvailable()
    {
        return _devices.Values
            .Where(device => device.Status == DeviceStatus.Online || device.Status == DeviceStatus.Busy)
            .ToList();
    }
}
