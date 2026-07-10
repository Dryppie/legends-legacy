using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Soulstones.UpgradeDefinition;
using MediatR;

namespace Application.UseCases.Soulstones.Commands.PurchaseSoulstoneUpgrade;

public record PurchaseSoulstoneUpgradeCommand(Guid CharacterId, string SoulstoneUpgradeId) : ICommand<Response<SoulstoneUpgradeMutationResult>>;

public class PurchaseSoulstoneUpgradeCommandHandler : IRequestHandler<PurchaseSoulstoneUpgradeCommand, Response<SoulstoneUpgradeMutationResult>>
{
    private readonly ISoulstoneUpgradeService _soulstoneUpgradeService;

    public PurchaseSoulstoneUpgradeCommandHandler(ISoulstoneUpgradeService soulstoneUpgradeService)
    {
        _soulstoneUpgradeService = soulstoneUpgradeService;
    }

    public Task<Response<SoulstoneUpgradeMutationResult>> Handle(PurchaseSoulstoneUpgradeCommand request, CancellationToken cancellationToken)
    {
        return _soulstoneUpgradeService.PurchaseAsync(request.CharacterId, request.SoulstoneUpgradeId, cancellationToken);
    }
}
