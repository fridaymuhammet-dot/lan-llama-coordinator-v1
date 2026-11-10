using LanLlamaCoordinator.Core.Models;
using LanLlamaCoordinator.Core.Services;
using TaskStatus = LanLlamaCoordinator.Core.Models.TaskStatus;

namespace LanLlamaCoordinator.Core.Tests;

public class TaskLifecycleTests
{
    [Fact]
    public void LifecycleService_ShouldTransitionQueuedToAssignedAndRunning()
    {
        var queueService = new TaskQueueService();
        var lifecycle = new TaskLifecycleService(queueService);

        var task = new TaskRecord
        {
            TaskId = "task-1",
            ModelName = "qwen2.5-coder-7b",
            Prompt = "deneme",
            Status = TaskStatus.Queued
        };

        lifecycle.Enqueue(task);
        lifecycle.Assign(task.TaskId, "device-1");
        lifecycle.Start(task.TaskId);

        var stored = queueService.Get(task.TaskId);
        Assert.NotNull(stored);
        Assert.Equal(TaskStatus.Running, stored!.Status);
        Assert.Equal("device-1", stored.AssignedDeviceId);
    }

    [Fact]
    public void RetryPolicy_ShouldStopAfterLimit()
    {
        var retryPolicy = new RetryPolicyService();
        var task = new TaskRecord { RetryCount = 3, MaxRetries = 3 };

        Assert.False(retryPolicy.ShouldRetry(task));
    }
}
