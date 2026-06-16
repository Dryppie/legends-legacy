using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.AcceptInvite;
public record AcceptInviteCommand(Guid CharacterId, string GuildId) : ICommand<Response<bool>>;
public class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    private readonly IGameEventPublisher _eventPublisher;

    public AcceptInviteCommandHandler(
        IGuildService guildService,
        IGameEventPublisher eventPublisher)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Response<bool>> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId)) return Response<bool>.Fail("Invalid guild.");

        var accepted = await _guildService.AcceptInviteAsync(request.CharacterId, guildId, cancellationToken);
        if (!accepted)
            return Response<bool>.Fail("Failed to accept invite.");

        await _eventPublisher.PublishAsync(
            new Audience.Character(request.CharacterId),
            new GuildMembershipChangedMsg(guildId, request.CharacterId));
        await _eventPublisher.PublishAsync(
            new Audience.Guild(guildId),
            new GuildStateChangedMsg(guildId));
        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new GuildDirectoryChangedMsg("membership"));

        return Response<bool>.Success(true);
    }
}
