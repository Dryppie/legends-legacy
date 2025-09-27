using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.AcceptInvite;
public record AcceptInviteCommand(Guid CharacterId, string GuildId) : ICommand<Response<bool>>;
public class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand, Response<bool>>
{
    private readonly IGuildService _guildService;

    public AcceptInviteCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task<Response<bool>> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId)) return Response<bool>.Fail("Invalid guild.");

        return await _guildService.AcceptInviteAsync(request.CharacterId, guildId, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to accept invite.");
    }
}