using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Core.Models;

public enum TaskStatus
{
    Queued,
    Assigned,
    Running,
    Succeeded,
    Failed,
    Retrying
}

public sealed class TaskRecord
{
    public string TaskId { get; set; } = Guid.NewGuid().ToString();
    public string ModelName { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public TaskStatus Status { get; set; } = TaskStatus.Queued;
    public string AssignedDeviceId { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedUtc { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string ResultSummary { get; set; } = string.Empty;
}
