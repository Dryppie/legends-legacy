using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.UpgradeGuildBuilding;
public record UpgradeGuildBuildingCommand(Guid CharacterId, string BuildingId) : ICommand<Response<bool>>;
public class UpgradeGuildBuildingCommandHandler : IRequestHandler<UpgradeGuildBuildingCommand, Response<bool>>
{
    private readonly IGuildBuildingUpgradeService _upgradeService;
    private readonly IGuildService _guildService;
    private readonly IGameEventPublisher _eventPublisher;

    public UpgradeGuildBuildingCommandHandler(
        IGuildBuildingUpgradeService upgradeService,
        IGuildService guildService,
        IGameEventPublisher eventPublisher)
    {
        _upgradeService = upgradeService;
        _guildService = guildService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Response<bool>> Handle(UpgradeGuildBuildingCommand request, CancellationToken cancellationToken)
    {
        var upgraded = await _upgradeService.PurchaseAsync(request.CharacterId, request.BuildingId, cancellationToken);
        if (!upgraded)
            return Response<bool>.Fail("Failed to upgrade guild building.");

        var guild = await _guildService.GetGuildWithUpgradesAsync(request.CharacterId, cancellationToken);
        if (guild != null)
        {
            await _eventPublisher.PublishAsync(
                new Audience.Guild(guild.Id),
                new GuildBuildingUpgradedMsg(guild.Id, request.BuildingId));
        }

        return Response<bool>.Success(true);
    }
}
