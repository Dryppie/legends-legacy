using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.RejectInvite;
public record RejectInviteCommand(Guid CharacterId, string GuildId) : ICommand<Response<bool>>;
public class RejectInviteCommandHandler : IRequestHandler<RejectInviteCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    private readonly IGameEventPublisher _eventPublisher;

    public RejectInviteCommandHandler(
        IGuildService guildService,
        IGameEventPublisher eventPublisher)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Response<bool>> Handle(RejectInviteCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId)) return Response<bool>.Fail("Invalid guild.");

        var rejected = await _guildService.RejectInviteAsync(request.CharacterId, guildId, cancellationToken);
        if (!rejected)
            return Response<bool>.Fail("Failed to reject invite");

        var msg = new GuildInviteRejectedMsg(guildId, request.CharacterId);
        await _eventPublisher.PublishAsync(new Audience.Character(request.CharacterId), msg);
        await _eventPublisher.PublishAsync(new Audience.Guild(guildId), msg);

        return Response<bool>.Success(true);
    }
}
