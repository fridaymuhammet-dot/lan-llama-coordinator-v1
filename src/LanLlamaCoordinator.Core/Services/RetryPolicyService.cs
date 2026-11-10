using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Core.Services;

public interface IRetryPolicyService
{
    bool ShouldRetry(TaskRecord task);
    TimeSpan GetDelay(TaskRecord task);
}

public sealed class RetryPolicyService : IRetryPolicyService
{
    public bool ShouldRetry(TaskRecord task)
    {
        return task.RetryCount < task.MaxRetries;
    }

    public TimeSpan GetDelay(TaskRecord task)
    {
        return TimeSpan.FromSeconds(Math.Min(30, 5 * (task.RetryCount + 1)));
    }
}
