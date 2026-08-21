using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.ApplyToGuild;
public record ApplyToGuildCommand(Guid CharacterId, string GuildId) : ICommand<Response<bool>>;
public class ApplyToGuildCommandHandler : IRequestHandler<ApplyToGuildCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    private readonly IGameRealtimeBroadcaster _eventPublisher;

    public ApplyToGuildCommandHandler(
        IGuildService guildService,
        IGameRealtimeBroadcaster eventPublisher)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Response<bool>> Handle(ApplyToGuildCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId)) return Response<bool>.Fail("Invalid guild");

        var applied = await _guildService.ApplyToGuildAsync(request.CharacterId, guildId, cancellationToken);
        if (!applied)
            return Response<bool>.Fail("Failed to apply to guild");

        await _eventPublisher.PublishAsync(
            new Audience.Guild(guildId),
            new GuildApplication(guildId, request.CharacterId),
            nameof(ApplyToGuildCommandHandler),
            cancellationToken);

        return Response<bool>.Success(true);
    }
}
