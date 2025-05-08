using Application.Interfaces.Services.LL;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.ApplyToGuild;
public record ApplyToGuildCommand(Guid CharacterId, string GuildId) : IRequest<Response<bool>>;
public class ApplyToGuildCommandHandler : IRequestHandler<ApplyToGuildCommand, Response<bool>>
{
    private readonly IGuildService _guildService;

    public ApplyToGuildCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task<Response<bool>> Handle(ApplyToGuildCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId)) return Response<bool>.Fail("Invalid guild");

        return await _guildService.ApplyToGuildAsync(request.CharacterId, guildId, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to reject application");
    }
}