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
    private readonly IGameEventPublisher _eventPublisher;

    public RejectApplicationCommandHandler(
        IGuildService guildService,
        IGameEventPublisher eventPublisher)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Response<bool>> Handle(RejectApplicationCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ApplicationCharacterId, out var applicationCharacterId)) return Response<bool>.Fail("Invalid character");

        var guild = await _guildService.GetGuildWithUpgradesAsync(request.CharacterId, cancellationToken);
        if (guild == null)
            return Response<bool>.Fail("Failed to reject application");

        var rejected = await _guildService.RejectApplicationAsync(request.CharacterId, applicationCharacterId, cancellationToken);
        if (!rejected)
            return Response<bool>.Fail("Failed to reject application");

        var msg = new GuildApplicationRejectedMsg(guild.Id, applicationCharacterId);
        await _eventPublisher.PublishAsync(new Audience.Character(applicationCharacterId), msg);
        await _eventPublisher.PublishAsync(new Audience.Guild(guild.Id), msg);

        return Response<bool>.Success(true);
    }
}
