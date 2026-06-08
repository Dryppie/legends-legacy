using Application.Interfaces.Services.LL.Dungeons;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Dungeons.Commands.ClaimDungeonRewards;

public record ClaimDungeonRewardsCommand(Guid CharacterId) : ICommand<Response<bool>>;

public class ClaimDungeonRewardsCommandHandler : IRequestHandler<ClaimDungeonRewardsCommand, Response<bool>>
{
    private readonly IDungeonRunService _dungeonRunService;

    public ClaimDungeonRewardsCommandHandler(IDungeonRunService dungeonRunService)
    {
        _dungeonRunService = dungeonRunService;
    }

    public async Task<Response<bool>> Handle(ClaimDungeonRewardsCommand request, CancellationToken cancellationToken)
    {
        var success = await _dungeonRunService.ClaimRewardsAsync(request.CharacterId, cancellationToken);

        return success
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("No completed dungeon run found.");
    }
}
