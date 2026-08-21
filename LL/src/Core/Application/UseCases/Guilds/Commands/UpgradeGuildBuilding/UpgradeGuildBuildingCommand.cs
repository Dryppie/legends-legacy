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
    private readonly IGameRealtimeBroadcaster _eventPublisher;
    private readonly IGuildSystemChatPublisher _guildChat;

    public UpgradeGuildBuildingCommandHandler(
        IGuildBuildingService guildBuildingService,
        IGameRealtimeBroadcaster eventPublisher,
        IGuildSystemChatPublisher guildChat)
    {
        _guildBuildingService = guildBuildingService;
        _eventPublisher = eventPublisher;
        _guildChat = guildChat;
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

        var building = result.Value.Buildings.Single(
            candidate => candidate.Id == request.BuildingId);
        await _guildChat.PublishBuildingAsync(
            result.Value.GuildId,
            request.CharacterId,
            building.Definition.Name,
            building.Level,
            GuildBuildingChatEvent.Upgraded,
            cancellationToken);

        await _eventPublisher.PublishAsync(
            new Audience.Guild(result.Value.GuildId),
            new GuildBuildingsChanged(result.Value.GuildId, request.BuildingId.ToString()),
            nameof(UpgradeGuildBuildingCommandHandler),
            cancellationToken);

        return Response<GuildBuildingOverviewDto>.Success(result.Value);
    }
}
