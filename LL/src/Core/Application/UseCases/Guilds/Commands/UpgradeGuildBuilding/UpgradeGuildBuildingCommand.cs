using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.UpgradeGuildBuilding;
public record UpgradeGuildBuildingCommand(Guid CharacterId, string BuildingId) : ICommand<Response<bool>>;
public class UpgradeGuildBuildingCommandHandler : IRequestHandler<UpgradeGuildBuildingCommand, Response<bool>>
{
    private readonly IGuildBuildingUpgradeService _upgradeService;

    public UpgradeGuildBuildingCommandHandler(IGuildBuildingUpgradeService upgradeService)
    {
        _upgradeService = upgradeService;
    }

    public async Task<Response<bool>> Handle(UpgradeGuildBuildingCommand request, CancellationToken cancellationToken)
    {
        return await _upgradeService.PurchaseAsync(request.CharacterId, request.BuildingId, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to upgrade guild building.");
    }
}
