using Application.Interfaces.Services.LL.Dungeons;
using Application.MediatR.Markers;
using Application.UseCases.Dungeons.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Dungeons.Commands.DismissFailedDungeonRun;

public record DismissFailedDungeonRunCommand(Guid CharacterId) : ICommand<Response<DismissFailedDungeonRunResponseDto>>;

public class DismissFailedDungeonRunCommandHandler : IRequestHandler<DismissFailedDungeonRunCommand, Response<DismissFailedDungeonRunResponseDto>>
{
    private readonly IDungeonRunService _dungeonRunService;

    public DismissFailedDungeonRunCommandHandler(IDungeonRunService dungeonRunService)
    {
        _dungeonRunService = dungeonRunService;
    }

    public async Task<Response<DismissFailedDungeonRunResponseDto>> Handle(DismissFailedDungeonRunCommand request, CancellationToken cancellationToken)
    {
        var success = await _dungeonRunService.DismissFailedRunAsync(request.CharacterId, cancellationToken);

        return success
            ? Response<DismissFailedDungeonRunResponseDto>.Success(new DismissFailedDungeonRunResponseDto
            {
                ActiveRun = null
            })
            : Response<DismissFailedDungeonRunResponseDto>.Fail("No failed dungeon run found.");
    }
}
