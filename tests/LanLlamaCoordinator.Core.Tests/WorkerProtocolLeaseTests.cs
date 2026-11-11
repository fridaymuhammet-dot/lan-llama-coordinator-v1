using LanLlamaCoordinator.Core.Contracts.Tasks;
using LanLlamaCoordinator.Core.Contracts.Workers;
using LanLlamaCoordinator.Infrastructure.WorkerProtocol;

namespace LanLlamaCoordinator.Core.Tests;

public class WorkerProtocolLeaseTests
{
    [Fact]
    public void ExpiredLeaseOnFinalAttemptFailsTaskAndIsProcessedOnlyOnce()
    {
        var protocol = new InMemoryWorkerCoordinatorProtocol();
        var registration = protocol.Register(new WorkerRegistrationRequest(
            "worker", "0.1.0", "host", new[] { "llama.cpp" },
            new WorkerCapabilitiesDto(8, 8_000_000_000, Array.Empty<WorkerGpuDto>()), "registration"));
        protocol.Approve(new WorkerApprovalRequest("registration", true));
        Assert.True(protocol.Heartbeat(new WorkerHeartbeatRequest(
            registration.WorkerId, 1, DateTimeOffset.UtcNow, WorkerRunStatus.Idle,
            new WorkerCapabilitiesDto(8, 8_000_000_000, Array.Empty<WorkerGpuDto>()), "heartbeat")).Accepted);
        Assert.True(protocol.Enqueue(new EnqueueTaskRequest(
            "last-attempt-task", "test-model", "llama.cpp", 1024, "hello", 32, MaxAttempts: 1)));

        var claim = protocol.ClaimTask(new TaskClaimRequest(
            registration.WorkerId, new[] { "llama.cpp" }, 4096, "claim"));
        Assert.True(claim.HasTask);
        Assert.Equal(1, claim.Task!.Attempt);

        var expiredAt = claim.LeaseExpiresAtUtc!.Value.AddSeconds(1);
        Assert.Equal(1, protocol.RequeueExpiredLeases(expiredAt));
        Assert.Equal(TaskWorkStatus.Failed, protocol.GetTaskStatus("last-attempt-task"));

        // A later sweep must not process a terminal task a second time.
        Assert.Equal(0, protocol.RequeueExpiredLeases(expiredAt.AddMinutes(1)));
        Assert.Equal(TaskWorkStatus.Failed, protocol.GetTaskStatus("last-attempt-task"));

        var lateResult = new TaskResultRequest("last-attempt-task", registration.WorkerId,
            claim.Task.LeaseId, claim.Task.Attempt, TaskResultStatus.Succeeded, "late", null, "late-result");
        Assert.False(protocol.SubmitResult(lateResult).Accepted);
    }
}
