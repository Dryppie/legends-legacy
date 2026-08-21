using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.DisbandGuild;
public record DisbandGuildCommand(Guid CharacterId) : ICommand<Response<bool>>;
public class DisbandGuildCommandHandler : IRequestHandler<DisbandGuildCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    private readonly IGameRealtimeBroadcaster _eventPublisher;

    public DisbandGuildCommandHandler(
        IGuildService guildService,
        IGameRealtimeBroadcaster eventPublisher)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Response<bool>> Handle(DisbandGuildCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildService.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        if (guild == null)
            return Response<bool>.Fail("Failed to disband guild");

        var disbanded = await _guildService.DisbandGuildAsync(request.CharacterId, cancellationToken);
        if (!disbanded)
            return Response<bool>.Fail("Failed to disband guild");

        await _eventPublisher.PublishAsync(
            new Audience.Guild(guild.Id),
            new GuildDisbanded(guild.Id),
            nameof(DisbandGuildCommandHandler),
            cancellationToken);
        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new GuildDirectoryChanged("disbanded"),
            nameof(DisbandGuildCommandHandler),
            cancellationToken);

        return Response<bool>.Success(true);
    }
}
