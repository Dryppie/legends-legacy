using Application.Interfaces.Services.LL.Nobility;
using Application.MediatR.Markers;
using MediatR;

namespace Application.UseCases.Nobility.Queries.CreateSignetCheckout;

// Alpha gateway has no side effects. Replace with an idempotent purchase command when checkout is enabled.
public sealed record CreateSignetCheckoutQuery(Guid AccountId, Guid CharacterId, string ProductId) : IQuery<NobilityCheckoutResult>;
public sealed class CreateSignetCheckoutQueryHandler(INobilityPurchaseGateway gateway)
    : IRequestHandler<CreateSignetCheckoutQuery, NobilityCheckoutResult>
{
    public Task<NobilityCheckoutResult> Handle(CreateSignetCheckoutQuery request, CancellationToken ct) =>
        gateway.CreateCheckoutAsync(request.AccountId, request.CharacterId, request.ProductId, ct);
}
