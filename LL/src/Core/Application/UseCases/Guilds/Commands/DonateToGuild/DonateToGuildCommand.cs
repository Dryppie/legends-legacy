using Application.Interfaces.Services.LL;
using Common.Primitives;
using Domain.Models.Guilds;
using MediatR;

namespace Application.UseCases.Guilds.Commands.DonateToGuild;
public record DonateToGuildCommand(Guid CharacterId, Dictionary<GuildResourceType, int> Donations) : IRequest<Response<bool>>;
public class DonateToGuildCommandHandler : IRequestHandler<DonateToGuildCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    public DonateToGuildCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }
    public async Task<Response<bool>> Handle(DonateToGuildCommand request, CancellationToken cancellationToken)
    {
        return await _guildService.DonateToGuildAsync(request.CharacterId, request.Donations, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to donate to guild.");
    }
}
