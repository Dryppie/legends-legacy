using Common.Primitives;
using Domain.Models.Administration;

namespace Application.Interfaces.Services.LL.Administration;
public interface IStateRefreshRecoveryService
{
    Task<Response<StateRefreshRecoveryPlan>> PrepareAsync(Guid operationId, Guid deliveryId, AdministrationActor actor, CancellationToken ct);
    Task<Response<StateRefreshRecoveryResult>> QueueAsync(Guid operationId, Guid deliveryId, AdministrationActor actor, string reason, CancellationToken ct);
}
