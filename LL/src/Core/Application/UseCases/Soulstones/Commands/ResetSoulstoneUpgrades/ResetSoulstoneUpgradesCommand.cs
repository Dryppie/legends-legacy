using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Soulstones.UpgradeDefinition;
using MediatR;

namespace Application.UseCases.Soulstones.Commands.ResetSoulstoneUpgrades;

public record ResetSoulstoneUpgradesCommand(Guid CharacterId) : ICommand<Response<SoulstoneUpgradeMutationResult>>;

public class ResetSoulstoneUpgradesCommandHandler : IRequestHandler<ResetSoulstoneUpgradesCommand, Response<SoulstoneUpgradeMutationResult>>
{
    private readonly ISoulstoneUpgradeService _soulstoneUpgradeService;

    public ResetSoulstoneUpgradesCommandHandler(ISoulstoneUpgradeService soulstoneUpgradeService)
    {
        _soulstoneUpgradeService = soulstoneUpgradeService;
    }

    public Task<Response<SoulstoneUpgradeMutationResult>> Handle(ResetSoulstoneUpgradesCommand request, CancellationToken cancellationToken)
    {
        return _soulstoneUpgradeService.ResetSoulstoneUpgradesAsync(request.CharacterId, cancellationToken);
    }
}
