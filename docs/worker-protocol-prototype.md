# Worker protocol prototype

The contract DTOs live under `LanLlamaCoordinator.Core.Contracts` and are intentionally separate from domain models. `IWorkerCoordinatorProtocol` is the transport-independent boundary; `InMemoryWorkerCoordinatorProtocol` is a local simulation implementation for unit tests.

This prototype supports the basic flow:

1. Worker submits a registration request and receives `PendingApproval`.
2. Coordinator explicitly approves or rejects the pending request.
3. Approved worker sends a monotonically increasing heartbeat.
4. Coordinator queues a task; a healthy worker claims work only when backend and memory requirements match.
5. The task is leased to that worker. Only the matching worker/lease/attempt may update status or submit a result.
6. Duplicate result submissions with the same idempotency key return the stored response. Expired leases are requeued or fail at the configured attempt limit.

## Important limitations

- This is in-memory simulation only; state is lost when the process exits.
- No HTTP server/client, TLS, pairing secret, credential authentication, persistence, real resource discovery, or inference engine is implemented.
- `WorkerId` is an opaque prototype identifier, not proof of identity. Never expose this implementation as a LAN service or treat it as a security boundary.
- The next step is to test this contract locally, then add a separately reviewed HTTPS transport and credential lifecycle before any LAN deployment.
