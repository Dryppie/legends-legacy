using Application.Interfaces.Services.LL.Dungeons;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Dungeons.Commands.DismissFailedDungeonRun;

public record DismissFailedDungeonRunCommand(Guid CharacterId) : ICommand<Response<bool>>;

public class DismissFailedDungeonRunCommandHandler : IRequestHandler<DismissFailedDungeonRunCommand, Response<bool>>
{
    private readonly IDungeonRunService _dungeonRunService;

    public DismissFailedDungeonRunCommandHandler(IDungeonRunService dungeonRunService)
    {
        _dungeonRunService = dungeonRunService;
    }

    public async Task<Response<bool>> Handle(DismissFailedDungeonRunCommand request, CancellationToken cancellationToken)
    {
        var success = await _dungeonRunService.DismissFailedRunAsync(request.CharacterId, cancellationToken);

        return success
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("No failed dungeon run found.");
    }
}
