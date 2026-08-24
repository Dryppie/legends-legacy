using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.RejectInvite;
public record RejectInviteCommand(Guid CharacterId, string GuildId) : ICommand<Response<bool>>;
public class RejectInviteCommandHandler : IRequestHandler<RejectInviteCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    public RejectInviteCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task<Response<bool>> Handle(RejectInviteCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId)) return Response<bool>.Fail("Invalid guild.");

        var rejected = await _guildService.RejectInviteAsync(request.CharacterId, guildId, cancellationToken);
        if (!rejected)
            return Response<bool>.Fail("Failed to reject invite");

        return Response<bool>.Success(true);
    }
}
