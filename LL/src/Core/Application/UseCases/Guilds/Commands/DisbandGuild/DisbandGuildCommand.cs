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
    private readonly IGameEventPublisher _eventPublisher;

    public DisbandGuildCommandHandler(
        IGuildService guildService,
        IGameEventPublisher eventPublisher)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Response<bool>> Handle(DisbandGuildCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildService.GetGuildWithUpgradesAsync(request.CharacterId, cancellationToken);
        if (guild == null)
            return Response<bool>.Fail("Failed to disband guild");

        var disbanded = await _guildService.DisbandGuildAsync(request.CharacterId, cancellationToken);
        if (!disbanded)
            return Response<bool>.Fail("Failed to disband guild");

        await _eventPublisher.PublishAsync(
            new Audience.Guild(guild.Id),
            new GuildDisbandedMsg(guild.Id));
        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new GuildDirectoryChangedMsg("disbanded"));

        return Response<bool>.Success(true);
    }
}
