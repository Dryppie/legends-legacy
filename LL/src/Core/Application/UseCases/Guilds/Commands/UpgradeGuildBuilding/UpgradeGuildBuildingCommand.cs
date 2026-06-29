using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.UpgradeGuildBuilding;

public record UpgradeGuildBuildingCommand(Guid CharacterId, Guid BuildingId) : ICommand<Response<GuildBuildingOverviewDto>>;

public class UpgradeGuildBuildingCommandHandler : IRequestHandler<UpgradeGuildBuildingCommand, Response<GuildBuildingOverviewDto>>
{
    private readonly IGuildBuildingService _guildBuildingService;
    private readonly IGameEventPublisher _eventPublisher;

    public UpgradeGuildBuildingCommandHandler(
        IGuildBuildingService guildBuildingService,
        IGameEventPublisher eventPublisher)
    {
        _guildBuildingService = guildBuildingService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Response<GuildBuildingOverviewDto>> Handle(UpgradeGuildBuildingCommand request, CancellationToken cancellationToken)
    {
        var result = await _guildBuildingService.UpgradeAsync(
            request.CharacterId,
            request.BuildingId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
            return Response<GuildBuildingOverviewDto>.Fail(result.Error ?? "Failed to upgrade guild building.");

        await _eventPublisher.PublishAsync(
            new Audience.Guild(result.Value.GuildId),
            new GuildBuildingsChangedMsg(result.Value.GuildId, request.BuildingId.ToString()));

        return Response<GuildBuildingOverviewDto>.Success(result.Value);
    }
}
