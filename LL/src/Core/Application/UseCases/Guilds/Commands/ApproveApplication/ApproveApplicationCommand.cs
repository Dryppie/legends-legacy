using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Guilds;
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
    private readonly IGameRealtimeBroadcaster _eventPublisher;
    private readonly IGuildSystemChatPublisher _guildChat;

    public ApproveApplicationCommandHandler(
        IGuildService guildService,
        IGameRealtimeBroadcaster eventPublisher,
        IGuildSystemChatPublisher guildChat)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
        _guildChat = guildChat;
    }

    public async Task<Response<bool>> Handle(ApproveApplicationCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ApplicationCharacterId, out var applicationCharacterId)) return Response<bool>.Fail("Invalid character");

        var guild = await _guildService.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        if (guild == null)
            return Response<bool>.Fail("Failed to approve application");

        var approved = await _guildService.ApproveApplicationAsync(request.CharacterId, applicationCharacterId, cancellationToken);
        if (!approved)
            return Response<bool>.Fail("Failed to approve application");

        await _guildChat.PublishAsync(
            guild.Id,
            applicationCharacterId,
            GuildSystemChatEvent.Joined,
            cancellationToken);

        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new GuildDirectoryChanged("membership", request.CharacterId),
            nameof(ApproveApplicationCommandHandler),
            cancellationToken);

        return Response<bool>.Success(true);
    }
}
