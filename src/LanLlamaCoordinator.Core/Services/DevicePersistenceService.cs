using System.Text.Json;
using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Core.Services;

public interface IDevicePersistenceService
{
    void Save(IEnumerable<DeviceInfo> devices, string filePath);
    List<DeviceInfo> Load(string filePath);
}

public sealed class DevicePersistenceService : IDevicePersistenceService
{
    public void Save(IEnumerable<DeviceInfo> devices, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(devices, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    public List<DeviceInfo> Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new List<DeviceInfo>();
        }

        var json = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<DeviceInfo>();
        }

        return JsonSerializer.Deserialize<List<DeviceInfo>>(json) ?? new List<DeviceInfo>();
    }
}
