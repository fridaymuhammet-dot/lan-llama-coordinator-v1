using System;
using System.Collections.Generic;

namespace LanLlamaCoordinator.Core.Models;

public enum DeviceType
{
    Laptop,
    Desktop,
    Phone
}

public enum NetworkType
{
    Ethernet,
    Wifi,
    Usb,
    Unknown
}

public enum DeviceStatus
{
    Offline,
    Online,
    Busy,
    Unhealthy,
    Maintenance
}

public sealed class DeviceCapability
{
    public bool SupportsLlamaCpp { get; set; }
    public bool SupportsRpc { get; set; }
    public int CpuCores { get; set; }
    public int RamGb { get; set; }
    public int VramGb { get; set; }
    public int MaxConcurrency { get; set; }
    public string? GpuName { get; set; }
}

public sealed class DeviceInfo
{
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DeviceType Type { get; set; }
    public string Platform { get; set; } = "windows";
    public NetworkType Network { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Offline;
    public DeviceCapability Capabilities { get; set; } = new();
    public bool Trusted { get; set; }
    public DateTimeOffset LastSeenUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool MaintenanceMode { get; set; }
}

public sealed class WorkerHeartbeat
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
    public DeviceStatus Status { get; set; } = DeviceStatus.Online;
    public int CpuPercent { get; set; }
    public int RamPercent { get; set; }
    public int GpuPercent { get; set; }
    public int TemperatureC { get; set; }
    public List<ModelInfo> Models { get; set; } = new();
}

public sealed class ModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string Format { get; set; } = "gguf";
    public bool Loaded { get; set; }
    public int MemoryMb { get; set; }
    public bool Compatible { get; set; }
}

public sealed class TaskRequest
{
    public string TaskId { get; set; } = Guid.NewGuid().ToString();
    public string ModelName { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 1024;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool RequiresGpu { get; set; }
    public int RequiredRamMb { get; set; }
}

public sealed class TaskAssignment
{
    public string TaskId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public DateTimeOffset AssignedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string State { get; set; } = "queued";
}
