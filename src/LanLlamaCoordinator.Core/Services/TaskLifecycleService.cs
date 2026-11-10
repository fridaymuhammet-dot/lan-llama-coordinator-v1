using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Core.Services;

public interface ITaskLifecycleService
{
    void Enqueue(TaskRecord task);
    void Assign(string taskId, string deviceId);
    void Start(string taskId);
    void Complete(string taskId, string summary);
    void Fail(string taskId, string reason);
    void Retry(string taskId);
    IReadOnlyCollection<TaskRecord> GetAll();
}

public sealed class TaskLifecycleService : ITaskLifecycleService
{
    private readonly TaskQueueService _queueService;

    public TaskLifecycleService(TaskQueueService queueService)
    {
        _queueService = queueService;
    }

    public void Enqueue(TaskRecord task)
    {
        task.Status = TaskStatus.Queued;
        task.UpdatedUtc = DateTimeOffset.UtcNow;
        _queueService.Enqueue(task);
    }

    public void Assign(string taskId, string deviceId)
    {
        var task = _queueService.Get(taskId);
        if (task is null)
        {
            return;
        }

        task.Status = TaskStatus.Assigned;
        task.AssignedDeviceId = deviceId;
        task.UpdatedUtc = DateTimeOffset.UtcNow;
        _queueService.Update(task);
    }

    public void Start(string taskId)
    {
        var task = _queueService.Get(taskId);
        if (task is null)
        {
            return;
        }

        task.Status = TaskStatus.Running;
        task.UpdatedUtc = DateTimeOffset.UtcNow;
        _queueService.Update(task);
    }

    public void Complete(string taskId, string summary)
    {
        var task = _queueService.Get(taskId);
        if (task is null)
        {
            return;
        }

        task.Status = TaskStatus.Succeeded;
        task.ResultSummary = summary;
        task.UpdatedUtc = DateTimeOffset.UtcNow;
        _queueService.Update(task);
    }

    public void Fail(string taskId, string reason)
    {
        var task = _queueService.Get(taskId);
        if (task is null)
        {
            return;
        }

        task.Status = TaskStatus.Failed;
        task.ResultSummary = reason;
        task.UpdatedUtc = DateTimeOffset.UtcNow;
        _queueService.Update(task);
    }

    public void Retry(string taskId)
    {
        var task = _queueService.Get(taskId);
        if (task is null)
        {
            return;
        }

        if (task.RetryCount >= task.MaxRetries)
        {
            task.Status = TaskStatus.Failed;
            task.ResultSummary = "Maksimum yeniden deneme sayısına ulaşıldı.";
            task.UpdatedUtc = DateTimeOffset.UtcNow;
            _queueService.Update(task);
            return;
        }

        task.RetryCount += 1;
        task.Status = TaskStatus.Retrying;
        task.UpdatedUtc = DateTimeOffset.UtcNow;
        _queueService.Update(task);
    }

    public IReadOnlyCollection<TaskRecord> GetAll() => _queueService.GetAll();
}
