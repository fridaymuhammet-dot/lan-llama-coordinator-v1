using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Core.Services;

public interface ITaskQueueService
{
    void Enqueue(TaskRecord task);
    TaskRecord? Dequeue(string taskId);
    IReadOnlyCollection<TaskRecord> GetAll();
    TaskRecord? Get(string taskId);
    void Update(TaskRecord task);
    void MarkAssigned(string taskId, string deviceId);
    void MarkRunning(string taskId);
    void MarkSucceeded(string taskId, string summary);
    void MarkFailed(string taskId, string reason);
}

public sealed class TaskQueueService : ITaskQueueService
{
    private readonly Dictionary<string, TaskRecord> _tasks = new(StringComparer.OrdinalIgnoreCase);

    public void Enqueue(TaskRecord task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _tasks[task.TaskId] = task;
    }

    public TaskRecord? Dequeue(string taskId)
    {
        if (_tasks.TryGetValue(taskId, out var task))
        {
            _tasks.Remove(taskId);
            return task;
        }

        return null;
    }

    public IReadOnlyCollection<TaskRecord> GetAll() => _tasks.Values.OrderBy(t => t.CreatedUtc).ToList();

    public TaskRecord? Get(string taskId)
    {
        return _tasks.TryGetValue(taskId, out var task) ? task : null;
    }

    public void Update(TaskRecord task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _tasks[task.TaskId] = task;
    }

    public void MarkAssigned(string taskId, string deviceId)
    {
        var task = Get(taskId);
        if (task is null)
        {
            return;
        }

        task.Status = TaskStatus.Assigned;
        task.AssignedDeviceId = deviceId;
        task.UpdatedUtc = DateTimeOffset.UtcNow;
    }

    public void MarkRunning(string taskId)
    {
        var task = Get(taskId);
        if (task is null)
        {
            return;
        }

        task.Status = TaskStatus.Running;
        task.UpdatedUtc = DateTimeOffset.UtcNow;
    }

    public void MarkSucceeded(string taskId, string summary)
    {
        var task = Get(taskId);
        if (task is null)
        {
            return;
        }

        task.Status = TaskStatus.Succeeded;
        task.ResultSummary = summary;
        task.UpdatedUtc = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string taskId, string reason)
    {
        var task = Get(taskId);
        if (task is null)
        {
            return;
        }

        task.Status = TaskStatus.Failed;
        task.ResultSummary = reason;
        task.UpdatedUtc = DateTimeOffset.UtcNow;
    }
}
