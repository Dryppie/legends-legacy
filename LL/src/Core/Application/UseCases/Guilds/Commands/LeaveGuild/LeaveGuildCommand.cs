using Application.Interfaces.Services.LL;
using MediatR;

namespace Application.UseCases.Guilds.Commands.LeaveGuild;
public record LeaveGuildCommand(Guid CharacterId) : IRequest;
public class LeaveGuildCommandHandler : IRequestHandler<LeaveGuildCommand>
{
    private readonly IGuildService _guildService;

    public LeaveGuildCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task Handle(LeaveGuildCommand request, CancellationToken cancellationToken)
    {
        await _guildService.LeaveGuildAsync(request.CharacterId, cancellationToken);
    }
}