using Application.Interfaces.Services.LL;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Soulstones.Commands;
public record PurchaseSoulstoneUpgradeCommand(Guid CharacterId, string SoulstoneUpgradeId) : IRequest<Response<bool>>;
public class PurchaseSoulstoneUpgradeCommandHandler : IRequestHandler<PurchaseSoulstoneUpgradeCommand, Response<bool>>
{
    private readonly ISoulstoneUpgradeService _soulstoneUpgradeService;

    public PurchaseSoulstoneUpgradeCommandHandler(ISoulstoneUpgradeService soulstoneUpgradeService)
    {
        _soulstoneUpgradeService = soulstoneUpgradeService;
    }

    public async Task<Response<bool>> Handle(PurchaseSoulstoneUpgradeCommand request, CancellationToken cancellationToken)
    {
        return await _soulstoneUpgradeService.PurchaseAsync(request.CharacterId, request.SoulstoneUpgradeId, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Could not purchase Soulstone Upgrade");
    }
}
