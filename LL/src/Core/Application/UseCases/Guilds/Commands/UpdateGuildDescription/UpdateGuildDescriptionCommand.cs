using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.Guilds.Dtos.Requests;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.UpdateGuildDescription;

public record UpdateGuildDescriptionCommand(Guid CharacterId, UpdateGuildDescriptionDto Request) : ICommand<Response<bool>>;

public class UpdateGuildDescriptionCommandHandler : IRequestHandler<UpdateGuildDescriptionCommand, Response<bool>>
{
    private readonly IGuildService _guild;
    private readonly IGameRealtimeBroadcaster _events;

    public UpdateGuildDescriptionCommandHandler(IGuildService guild, IGameRealtimeBroadcaster events)
    {
        _guild = guild;
        _events = events;
    }

    public async Task<Response<bool>> Handle(UpdateGuildDescriptionCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guild.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        var updated = await _guild.UpdateDescriptionAsync(
            request.CharacterId,
            request.Request.Description,
            cancellationToken);
        if (!updated || guild is null)
            return Response<bool>.Fail("Only the guild leader and officers can change the guild description.");

        await _events.PublishAsync(new Audience.Guild(guild.Id), new GuildStateChanged(guild.Id), nameof(UpdateGuildDescriptionCommandHandler), cancellationToken);
        return Response<bool>.Success(true);
    }
}
