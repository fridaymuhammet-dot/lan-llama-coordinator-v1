using LanLlamaCoordinator.Core.Contracts.Workers;
using LanLlamaCoordinator.Infrastructure.WorkerProtocol;

namespace LanLlamaCoordinator.Core.Tests;

public class WorkerRegistrationIdempotencyTests
{
    [Fact]
    public void ReplayingApprovedRegistrationReturnsSameWorkerAndApprovalState()
    {
        var protocol = new InMemoryWorkerCoordinatorProtocol();
        var request = CreateRegistration("registration-replay");
        var first = protocol.Register(request);
        var approval = protocol.Approve(new WorkerApprovalRequest(request.RequestId, true));

        var replay = protocol.Register(request);

        Assert.Equal(WorkerRegistrationStatus.Approved, approval.Status);
        Assert.Equal(first.WorkerId, replay.WorkerId);
        Assert.Equal(WorkerRegistrationStatus.Approved, replay.Status);
        Assert.Null(replay.Error);
    }

    [Fact]
    public void ReusingRegistrationRequestIdWithDifferentPayloadIsRejected()
    {
        var protocol = new InMemoryWorkerCoordinatorProtocol();
        var first = protocol.Register(CreateRegistration("registration-conflict"));
        var conflictingRequest = CreateRegistration("registration-conflict") with
        {
            WorkerName = "different-worker"
        };

        var conflict = protocol.Register(conflictingRequest);

        Assert.Equal(WorkerRegistrationStatus.InvalidRequest, conflict.Status);
        Assert.Equal("registration_idempotency_conflict", conflict.Error?.Code);
        Assert.NotEqual(first.WorkerId, conflict.WorkerId);
    }

    private static WorkerRegistrationRequest CreateRegistration(string requestId) => new(
        "worker", "0.1.0", "host", new[] { "llama.cpp" },
        new WorkerCapabilitiesDto(8, 8_000_000_000, Array.Empty<WorkerGpuDto>()), requestId);
}
