using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Markers;
using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Application.UseCases.Guilds.Dtos.Requests;
using Common.Primitives;
using Domain.Models.Guilds;
using MediatR;

namespace Application.UseCases.Guilds.Commands.ChangeGuildMemberRole;

public record ChangeGuildMemberRoleCommand(Guid CharacterId, ChangeGuildMemberRoleDto Request) : ICommand<Response<bool>>;

public class ChangeGuildMemberRoleCommandHandler : IRequestHandler<ChangeGuildMemberRoleCommand, Response<bool>>
{
    private readonly IGuildService _guild;
    private readonly IGameRealtimeBroadcaster _events;
    private readonly IGuildSystemChatPublisher _guildChat;
    public ChangeGuildMemberRoleCommandHandler(
        IGuildService guild,
        IGameRealtimeBroadcaster events,
        IGuildSystemChatPublisher guildChat)
    {
        _guild = guild;
        _events = events;
        _guildChat = guildChat;
    }

    public async Task<Response<bool>> Handle(ChangeGuildMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guild.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        var previousRole = guild?.Members
            .FirstOrDefault(member => member.CharacterId == request.Request.CharacterId)
            ?.Role;
        var changed = await _guild.ChangeMemberRoleAsync(request.CharacterId, request.Request.CharacterId, request.Request.Role, cancellationToken);
        if (!changed || guild is null) return Response<bool>.Fail("You cannot change that member's role.");

        var chatEvent = (previousRole, request.Request.Role) switch
        {
            (GuildRole.Member, GuildRole.Officer) => GuildSystemChatEvent.PromotedToOfficer,
            (GuildRole.Officer, GuildRole.Member) => GuildSystemChatEvent.DemotedToMember,
            _ => (GuildSystemChatEvent?)null
        };
        if (chatEvent.HasValue)
        {
            await _guildChat.PublishAsync(
                guild.Id,
                request.Request.CharacterId,
                chatEvent.Value,
                cancellationToken);
        }

        await _events.PublishAsync(new Audience.Guild(guild.Id), new GuildStateChanged(guild.Id, request.CharacterId, true), nameof(ChangeGuildMemberRoleCommandHandler), cancellationToken);
        return Response<bool>.Success(true);
    }
}
