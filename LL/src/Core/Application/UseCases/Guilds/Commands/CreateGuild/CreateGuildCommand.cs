using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.CreateGuild;
public record CreateGuildCommand(Guid CharacterId, string Name) : ICommand<Response<bool>>;

public record CreateGuildCommandHandler : IRequestHandler<CreateGuildCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    private readonly IGameRealtimeBroadcaster _eventPublisher;

    public CreateGuildCommandHandler(
        IGuildService guildService,
        IGameRealtimeBroadcaster eventPublisher)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Response<bool>> Handle(CreateGuildCommand request, CancellationToken cancellationToken)
    {
        var created = await _guildService.CreateAsync(request.CharacterId, request.Name, cancellationToken);
        if (!created)
            return Response<bool>.Fail("Could not create guild.");

        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new GuildDirectoryChanged("created"),
            nameof(CreateGuildCommandHandler),
            cancellationToken);

        return Response<bool>.Success(true);
    }
}
