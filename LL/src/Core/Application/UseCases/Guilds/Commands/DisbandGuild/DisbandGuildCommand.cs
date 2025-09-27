using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.DisbandGuild;
public record DisbandGuildCommand(Guid CharacterId) : ICommand<Response<bool>>;
public class DisbandGuildCommandHandler : IRequestHandler<DisbandGuildCommand, Response<bool>>
{
    private readonly IGuildService _guildService;

    public DisbandGuildCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task<Response<bool>> Handle(DisbandGuildCommand request, CancellationToken cancellationToken)
    {
        return await _guildService.DisbandGuildAsync(request.CharacterId, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to reject application");
    }
}