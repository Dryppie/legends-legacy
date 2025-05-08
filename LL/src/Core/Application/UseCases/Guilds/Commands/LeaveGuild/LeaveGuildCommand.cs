using Application.Interfaces.Services.LL;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.LeaveGuild;
public record LeaveGuildCommand(Guid CharacterId) : IRequest<Response<bool>>;
public class LeaveGuildCommandHandler : IRequestHandler<LeaveGuildCommand, Response<bool>>
{
    private readonly IGuildService _guildService;

    public LeaveGuildCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task<Response<bool>> Handle(LeaveGuildCommand request, CancellationToken cancellationToken)
    {
        return await _guildService.LeaveGuildAsync(request.CharacterId, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to reject application");
    }
}