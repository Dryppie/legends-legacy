using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using Domain.Models.Guilds.Buildings;
using MediatR;

namespace Application.UseCases.Guilds.Commands.ConstructGuildBuilding;

public record ConstructGuildBuildingCommand(Guid CharacterId, GuildBuildingType BuildingType) : ICommand<Response<GuildBuildingOverviewDto>>;

public class ConstructGuildBuildingCommandHandler : IRequestHandler<ConstructGuildBuildingCommand, Response<GuildBuildingOverviewDto>>
{
    private readonly IGuildBuildingService _guildBuildingService;
    private readonly IGameRealtimeBroadcaster _eventPublisher;
    private readonly IGuildSystemChatPublisher _guildChat;

    public ConstructGuildBuildingCommandHandler(
        IGuildBuildingService guildBuildingService,
        IGameRealtimeBroadcaster eventPublisher,
        IGuildSystemChatPublisher guildChat)
    {
        _guildBuildingService = guildBuildingService;
        _eventPublisher = eventPublisher;
        _guildChat = guildChat;
    }

    public async Task<Response<GuildBuildingOverviewDto>> Handle(ConstructGuildBuildingCommand request, CancellationToken cancellationToken)
    {
        var result = await _guildBuildingService.ConstructAsync(
            request.CharacterId,
            request.BuildingType,
            DateTimeOffset.UtcNow,
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
            return Response<GuildBuildingOverviewDto>.Fail(result.Error ?? "Failed to construct guild building.");

        var building = result.Value.Buildings.Single(
            candidate => candidate.Definition.Type == request.BuildingType);
        await _guildChat.PublishBuildingAsync(
            result.Value.GuildId,
            request.CharacterId,
            building.Definition.Name,
            building.Level,
            GuildBuildingChatEvent.Constructed,
            cancellationToken);

        await _eventPublisher.PublishAsync(
            new Audience.Guild(result.Value.GuildId),
            new GuildBuildingsChanged(result.Value.GuildId, request.BuildingType.ToString()),
            nameof(ConstructGuildBuildingCommandHandler),
            cancellationToken);

        return Response<GuildBuildingOverviewDto>.Success(result.Value);
    }
}
