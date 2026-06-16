using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.ApproveApplication;
public record ApproveApplicationCommand(Guid CharacterId, string ApplicationCharacterId) : ICommand<Response<bool>>;
public class ApproveApplicationCommandHandler : IRequestHandler<ApproveApplicationCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    private readonly IGameEventPublisher _eventPublisher;

    public ApproveApplicationCommandHandler(
        IGuildService guildService,
        IGameEventPublisher eventPublisher)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Response<bool>> Handle(ApproveApplicationCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ApplicationCharacterId, out var applicationCharacterId)) return Response<bool>.Fail("Invalid character");

        var guild = await _guildService.GetGuildWithUpgradesAsync(request.CharacterId, cancellationToken);
        if (guild == null)
            return Response<bool>.Fail("Failed to approve application");

        var approved = await _guildService.ApproveApplicationAsync(request.CharacterId, applicationCharacterId, cancellationToken);
        if (!approved)
            return Response<bool>.Fail("Failed to approve application");

        await _eventPublisher.PublishAsync(
            new Audience.Character(applicationCharacterId),
            new GuildMembershipChangedMsg(guild.Id, applicationCharacterId));
        await _eventPublisher.PublishAsync(
            new Audience.Guild(guild.Id),
            new GuildStateChangedMsg(guild.Id));
        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new GuildDirectoryChangedMsg("membership"));

        return Response<bool>.Success(true);
    }
}
