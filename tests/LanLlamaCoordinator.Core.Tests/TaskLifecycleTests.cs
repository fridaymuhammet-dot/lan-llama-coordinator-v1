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
    public void Retry_ShouldIncrementCountAndClearPreviousAttemptData()
    {
        var queueService = new TaskQueueService();
        var lifecycle = new TaskLifecycleService(queueService);
        var task = new TaskRecord
        {
            TaskId = "task-retry",
            AssignedDeviceId = "worker-1",
            ResultSummary = "previous error",
            MaxRetries = 2
        };
        lifecycle.Enqueue(task);
        lifecycle.Assign(task.TaskId, "worker-1");

        lifecycle.Retry(task.TaskId);

        var stored = queueService.Get(task.TaskId);
        Assert.NotNull(stored);
        Assert.Equal(TaskStatus.Retrying, stored!.Status);
        Assert.Equal(1, stored.RetryCount);
        Assert.Empty(stored.AssignedDeviceId);
        Assert.Empty(stored.ResultSummary);
        Assert.NotNull(stored.UpdatedUtc);
    }

    [Fact]
    public void Retry_ShouldFailTaskWhenMaximumRetriesReached()
    {
        var queueService = new TaskQueueService();
        var lifecycle = new TaskLifecycleService(queueService);
        var task = new TaskRecord
        {
            TaskId = "task-no-retries-left",
            RetryCount = 2,
            MaxRetries = 2,
            AssignedDeviceId = "worker-1"
        };
        lifecycle.Enqueue(task);
        task.RetryCount = 2;
        task.AssignedDeviceId = "worker-1";
        queueService.Update(task);

        lifecycle.Retry(task.TaskId);

        var stored = queueService.Get(task.TaskId);
        Assert.NotNull(stored);
        Assert.Equal(TaskStatus.Failed, stored!.Status);
        Assert.Equal(2, stored.RetryCount);
        Assert.Empty(stored.AssignedDeviceId);
        Assert.Equal("Maximum retry attempts reached.", stored.ResultSummary);
    }

    [Fact]
    public void CompleteAndFail_ShouldClearAssignedDevice()
    {
        var queueService = new TaskQueueService();
        var lifecycle = new TaskLifecycleService(queueService);
        var succeeded = new TaskRecord { TaskId = "task-success" };
        var failed = new TaskRecord { TaskId = "task-failure" };
        lifecycle.Enqueue(succeeded);
        lifecycle.Enqueue(failed);
        lifecycle.Assign(succeeded.TaskId, "worker-1");
        lifecycle.Assign(failed.TaskId, "worker-2");

        lifecycle.Complete(succeeded.TaskId, "completed");
        lifecycle.Fail(failed.TaskId, "worker failed");

        Assert.Equal(TaskStatus.Succeeded, queueService.Get(succeeded.TaskId)!.Status);
        Assert.Empty(queueService.Get(succeeded.TaskId)!.AssignedDeviceId);
        Assert.Equal("completed", queueService.Get(succeeded.TaskId)!.ResultSummary);
        Assert.Equal(TaskStatus.Failed, queueService.Get(failed.TaskId)!.Status);
        Assert.Empty(queueService.Get(failed.TaskId)!.AssignedDeviceId);
        Assert.Equal("worker failed", queueService.Get(failed.TaskId)!.ResultSummary);
    }

    [Fact]
    public void RetryPolicy_ShouldStopAfterLimit()
    {
        var retryPolicy = new RetryPolicyService();
        var task = new TaskRecord { RetryCount = 3, MaxRetries = 3 };

        Assert.False(retryPolicy.ShouldRetry(task));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(3, 30)]
    [InlineData(10, 30)]
    public void RetryPolicy_ShouldUseBoundedExponentialBackoff(int retryCount, int expectedSeconds)
    {
        var retryPolicy = new RetryPolicyService();
        var task = new TaskRecord { RetryCount = retryCount, MaxRetries = 10 };

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), retryPolicy.GetDelay(task));
    }

    [Fact]
    public void RetryPolicy_ShouldRejectNegativeRetryCountForDelay()
    {
        var retryPolicy = new RetryPolicyService();
        var task = new TaskRecord { RetryCount = -1, MaxRetries = 3 };

        Assert.Throws<ArgumentOutOfRangeException>(() => retryPolicy.GetDelay(task));
    }
}
