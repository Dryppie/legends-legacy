using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Common.Primitives;
using Domain.Models.Guilds.Buildings;
using MediatR;

namespace Application.UseCases.Guilds.Commands.SetGuildBuildingTarget;

public sealed record SetGuildBuildingTargetCommand(
    Guid CharacterId,
    GuildBuildingType BuildingType) : ICommand<Response<GuildBuildingOverviewDto>>;

public sealed class SetGuildBuildingTargetCommandHandler
    : IRequestHandler<SetGuildBuildingTargetCommand, Response<GuildBuildingOverviewDto>>
{
    private readonly IGuildBuildingService _guildBuildingService;
    private readonly ICharacterService _characters;
    private readonly IGameEventPublisher _events;
    private readonly IGameEventOutbox _outbox;

    public SetGuildBuildingTargetCommandHandler(
        IGuildBuildingService guildBuildingService,
        ICharacterService characters,
        IGameEventPublisher events,
        IGameEventOutbox outbox)
    {
        _guildBuildingService = guildBuildingService;
        _characters = characters;
        _events = events;
        _outbox = outbox;
    }

    public async Task<Response<GuildBuildingOverviewDto>> Handle(
        SetGuildBuildingTargetCommand request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var result = await _guildBuildingService.SetCurrentTargetAsync(
            request.CharacterId,
            request.BuildingType,
            now,
            cancellationToken);

        if (!result.Succeeded || result.Value?.CurrentTarget is null)
            return Response<GuildBuildingOverviewDto>.Fail(
                result.Error ?? "Failed to set the guild building target.");

        var actor = await _characters.GetBaseCharacterByIdAsync(
            request.CharacterId,
            cancellationToken);
        if (actor is null)
            return Response<GuildBuildingOverviewDto>.Fail("Character was not found.");

        var target = result.Value.CurrentTarget;
        var messageId = Guid.NewGuid();
        await _outbox.EnqueueAsync(
            GameEventTypes.GuildChatMessage,
            new GuildChatMessagePayload(
                result.Value.GuildId,
                actor.Id,
                actor.Name,
                $"Set the current building target to {target.Name} level {target.TargetLevel}.",
                messageId,
                now),
            actor.Id,
            actor.UserId,
            cancellationToken);

        await _events.PublishAsync(
            new Audience.Guild(result.Value.GuildId),
            new GuildBuildingsChangedMsg(
                result.Value.GuildId,
                request.BuildingType.ToString()));

        return Response<GuildBuildingOverviewDto>.Success(result.Value);
    }
}
