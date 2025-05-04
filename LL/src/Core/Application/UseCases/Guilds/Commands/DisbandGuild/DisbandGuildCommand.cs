using Application.Interfaces.Services.LL;
using MediatR;

namespace Application.UseCases.Guilds.Commands.DisbandGuild;
public record DisbandGuildCommand(Guid CharacterId) : IRequest;
public class DisbandGuildCommandHandler : IRequestHandler<DisbandGuildCommand>
{
    private readonly IGuildService _guildService;

    public DisbandGuildCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task Handle(DisbandGuildCommand request, CancellationToken cancellationToken)
    {
        await _guildService.DisbandGuildAsync(request.CharacterId, cancellationToken);
    }
}