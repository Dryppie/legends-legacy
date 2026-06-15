using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.LeaveGuild;
public record LeaveGuildCommand(Guid CharacterId) : ICommand<Response<bool>>;
public class LeaveGuildCommandHandler : IRequestHandler<LeaveGuildCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    private readonly IGameEventPublisher _eventPublisher;

    public LeaveGuildCommandHandler(
        IGuildService guildService,
        IGameEventPublisher eventPublisher)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Response<bool>> Handle(LeaveGuildCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildService.GetGuildWithUpgradesAsync(request.CharacterId, cancellationToken);
        if (guild == null)
            return Response<bool>.Fail("Failed to leave guild");

        var left = await _guildService.LeaveGuildAsync(request.CharacterId, cancellationToken);
        if (!left)
            return Response<bool>.Fail("Failed to leave guild");

        await _eventPublisher.PublishAsync(
            new Audience.Character(request.CharacterId),
            new GuildMembershipChangedMsg(guild.Id, request.CharacterId));
        await _eventPublisher.PublishAsync(
            new Audience.Guild(guild.Id),
            new GuildStateChangedMsg(guild.Id));
        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new GuildDirectoryChangedMsg("membership"));

        return Response<bool>.Success(true);
    }
}
