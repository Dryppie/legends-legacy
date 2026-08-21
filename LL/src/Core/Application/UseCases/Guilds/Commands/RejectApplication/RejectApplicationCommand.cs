using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.RejectApplication;
public record RejectApplicationCommand(Guid CharacterId, string ApplicationCharacterId) : ICommand<Response<bool>>;
public class RejectApplicationCommandHandler : IRequestHandler<RejectApplicationCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    private readonly IGameRealtimeBroadcaster _eventPublisher;

    public RejectApplicationCommandHandler(
        IGuildService guildService,
        IGameRealtimeBroadcaster eventPublisher)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Response<bool>> Handle(RejectApplicationCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ApplicationCharacterId, out var applicationCharacterId)) return Response<bool>.Fail("Invalid character");

        var guild = await _guildService.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        if (guild == null)
            return Response<bool>.Fail("Failed to reject application");

        var rejected = await _guildService.RejectApplicationAsync(request.CharacterId, applicationCharacterId, cancellationToken);
        if (!rejected)
            return Response<bool>.Fail("Failed to reject application");

        var message = new GuildApplicationRejected(guild.Id, applicationCharacterId);
        await _eventPublisher.PublishAsync(new Audience.Character(applicationCharacterId), message, nameof(RejectApplicationCommandHandler), cancellationToken);
        await _eventPublisher.PublishAsync(new Audience.Guild(guild.Id), message, nameof(RejectApplicationCommandHandler), cancellationToken);

        return Response<bool>.Success(true);
    }
}
