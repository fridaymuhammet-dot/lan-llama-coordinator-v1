using LanLlamaCoordinator.Core.Contracts.Tasks;
using LanLlamaCoordinator.Core.Contracts.Workers;
using LanLlamaCoordinator.Infrastructure.WorkerProtocol;

namespace LanLlamaCoordinator.Core.Tests;

public class WorkerProtocolValidationTests
{
    [Fact]
    public void RegistrationWithInvalidCapabilitiesIsRejected()
    {
        var protocol = new InMemoryWorkerCoordinatorProtocol();
        var request = new WorkerRegistrationRequest(
            "worker", "0.1.0", "host", new[] { "llama.cpp" },
            new WorkerCapabilitiesDto(0, 1024, Array.Empty<WorkerGpuDto>()), "invalid-capabilities");

        var response = protocol.Register(request);

        Assert.Equal(WorkerRegistrationStatus.InvalidRequest, response.Status);
        Assert.Equal("invalid_registration", response.Error?.Code);
    }

    [Fact]
    public void HeartbeatWithInvalidGpuCapacityIsRejectedWithoutUpdatingWorker()
    {
        var protocol = new InMemoryWorkerCoordinatorProtocol();
        var registrationRequest = ValidRegistration("invalid-gpu-heartbeat");
        var registration = protocol.Register(registrationRequest);
        protocol.Approve(new WorkerApprovalRequest(registrationRequest.RequestId, true));
        var invalidCapacity = new WorkerCapabilitiesDto(8, 4096,
            new[] { new WorkerGpuDto("GPU", 1024, 2048, "llama.cpp") });

        var response = protocol.Heartbeat(new WorkerHeartbeatRequest(
            registration.WorkerId, 1, DateTimeOffset.UtcNow, WorkerRunStatus.Idle,
            invalidCapacity, "invalid-gpu-heartbeat-request"));

        Assert.False(response.Accepted);
        Assert.Equal("invalid_capacity", response.Error?.Code);
        Assert.Equal(WorkerRunStatus.Unhealthy, protocol.GetWorkerStatus(registration.WorkerId));
    }

    [Fact]
    public void InvalidTaskParametersAreRejectedWithoutEnqueueing()
    {
        var protocol = new InMemoryWorkerCoordinatorProtocol();

        var accepted = protocol.Enqueue(new EnqueueTaskRequest(
            "invalid-task", "model", "llama.cpp", -1, "prompt", 32));

        Assert.False(accepted);
        Assert.Null(protocol.GetTaskStatus("invalid-task"));
    }

    private static WorkerRegistrationRequest ValidRegistration(string requestId) => new(
        "worker", "0.1.0", "host", new[] { "llama.cpp" },
        new WorkerCapabilitiesDto(8, 8_000_000_000, Array.Empty<WorkerGpuDto>()), requestId);
}
