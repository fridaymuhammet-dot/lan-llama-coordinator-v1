using LanLlamaCoordinator.Core.Contracts.Tasks;
using LanLlamaCoordinator.Core.Contracts.Workers;

namespace LanLlamaCoordinator.Infrastructure.WorkerProtocol;

/// <summary>Transport-independent worker/coordinator contract. The in-memory implementation is for tests and local simulation only.</summary>
public interface IWorkerCoordinatorProtocol
{
    WorkerRegistrationResponse Register(WorkerRegistrationRequest request);
    WorkerApprovalResponse Approve(WorkerApprovalRequest request);
    WorkerHeartbeatResponse Heartbeat(WorkerHeartbeatRequest request);
    bool Enqueue(EnqueueTaskRequest task);
    TaskClaimResponse ClaimTask(TaskClaimRequest request);
    TaskStatusUpdateResponse UpdateTaskStatus(TaskStatusUpdateRequest request);
    TaskResultResponse SubmitResult(TaskResultRequest request);
    int RequeueExpiredLeases(DateTimeOffset nowUtc);
    TaskWorkStatus? GetTaskStatus(string taskId);
    WorkerRunStatus? GetWorkerStatus(string workerId);
}
