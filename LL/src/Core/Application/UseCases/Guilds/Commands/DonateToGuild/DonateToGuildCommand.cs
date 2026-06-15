using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using Domain.Models.Guilds;
using MediatR;

namespace Application.UseCases.Guilds.Commands.DonateToGuild;
public record DonateToGuildCommand(Guid CharacterId, Dictionary<GuildResourceType, int> Donations) : ICommand<Response<bool>>;
public class DonateToGuildCommandHandler : IRequestHandler<DonateToGuildCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    private readonly IGameEventPublisher _eventPublisher;

    public DonateToGuildCommandHandler(
        IGuildService guildService,
        IGameEventPublisher eventPublisher)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Response<bool>> Handle(DonateToGuildCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildService.GetGuildWithUpgradesAsync(request.CharacterId, cancellationToken);
        if (guild == null)
            return Response<bool>.Fail("Failed to donate to guild.");

        var donated = await _guildService.DonateToGuildAsync(request.CharacterId, request.Donations, cancellationToken);
        if (!donated)
            return Response<bool>.Fail("Failed to donate to guild.");

        await _eventPublisher.PublishAsync(
            new Audience.Guild(guild.Id),
            new GuildStateChangedMsg(guild.Id));

        return Response<bool>.Success(true);
    }
}
