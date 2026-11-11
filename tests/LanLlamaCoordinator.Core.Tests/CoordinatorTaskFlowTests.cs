using LanLlamaCoordinator.Core.Contracts.Tasks;
using LanLlamaCoordinator.Core.Contracts.Workers;
using LanLlamaCoordinator.Infrastructure.WorkerProtocol;

namespace LanLlamaCoordinator.Core.Tests;

public class CoordinatorTaskFlowTests
{
    [Fact]
    public void ApprovedWorkerCanRunTaskAndReportSuccessfulResult()
    {
        var protocol = new InMemoryWorkerCoordinatorProtocol();
        var capabilities = new WorkerCapabilitiesDto(8, 8_000_000_000, Array.Empty<WorkerGpuDto>());
        var registration = protocol.Register(new WorkerRegistrationRequest(
            "integration-worker", "0.1.0", "test-host", new[] { "llama.cpp" }, capabilities, "register-flow"));

        Assert.Equal(WorkerRegistrationStatus.PendingApproval, registration.Status);
        Assert.Equal(WorkerRegistrationStatus.Approved,
            protocol.Approve(new WorkerApprovalRequest("register-flow", true)).Status);
        Assert.True(protocol.Heartbeat(new WorkerHeartbeatRequest(
            registration.WorkerId, 1, DateTimeOffset.UtcNow, WorkerRunStatus.Idle, capabilities, "heartbeat-flow")).Accepted);
        Assert.True(protocol.Enqueue(new EnqueueTaskRequest(
            "flow-task", "test-model", "llama.cpp", 1024, "Say hello", 32)));

        var claim = protocol.ClaimTask(new TaskClaimRequest(
            registration.WorkerId, new[] { "llama.cpp" }, capabilities.AvailableMemoryBytes, "claim-flow"));

        Assert.True(claim.HasTask);
        Assert.NotNull(claim.Task);
        Assert.Equal("flow-task", claim.Task!.TaskId);
        Assert.Equal(TaskWorkStatus.Leased, protocol.GetTaskStatus("flow-task"));

        var started = protocol.UpdateTaskStatus(new TaskStatusUpdateRequest(
            claim.Task.TaskId, registration.WorkerId, claim.Task.LeaseId,
            TaskWorkStatus.Running, "status-running"));
        Assert.True(started.Accepted);
        Assert.Equal(TaskWorkStatus.Running, protocol.GetTaskStatus("flow-task"));

        var result = new TaskResultRequest(
            claim.Task.TaskId, registration.WorkerId, claim.Task.LeaseId, claim.Task.Attempt,
            TaskResultStatus.Succeeded, "Hello!", null, "result-flow");
        Assert.True(protocol.SubmitResult(result).Accepted);
        Assert.Equal(TaskWorkStatus.Succeeded, protocol.GetTaskStatus("flow-task"));
    }
}
