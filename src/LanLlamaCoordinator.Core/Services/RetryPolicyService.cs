using LanLlamaCoordinator.Core.Models;

namespace LanLlamaCoordinator.Core.Services;

public interface IRetryPolicyService
{
    bool ShouldRetry(TaskRecord task);
    TimeSpan GetDelay(TaskRecord task);
}

/// <summary>Provides bounded exponential backoff for task retries.</summary>
public sealed class RetryPolicyService : IRetryPolicyService
{
    private static readonly TimeSpan MaximumDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(5);

    public bool ShouldRetry(TaskRecord task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return task.RetryCount >= 0 && task.MaxRetries >= 0 && task.RetryCount < task.MaxRetries;
    }

    public TimeSpan GetDelay(TaskRecord task)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (task.RetryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(task), "Retry count cannot be negative.");
        }

        // RetryCount counts retries already performed. The next delay doubles each time,
        // and saturates before arithmetic can overflow for unusually large counts.
        var delay = InitialDelay;
        for (var retry = 0; retry < task.RetryCount && delay < MaximumDelay; retry++)
        {
            delay = TimeSpan.FromTicks(Math.Min(MaximumDelay.Ticks, delay.Ticks * 2));
        }

        return delay;
    }
}
