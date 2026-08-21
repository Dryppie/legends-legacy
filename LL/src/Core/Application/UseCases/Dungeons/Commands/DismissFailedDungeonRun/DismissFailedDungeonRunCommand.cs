using Application.Interfaces.Services.LL.Dungeons;
using Application.MediatR.Markers;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Dungeons.Queries.GetAvailableDungeons;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Dungeons.Commands.DismissFailedDungeonRun;

public record DismissFailedDungeonRunCommand(Guid CharacterId) : ICommand<Response<DismissFailedDungeonRunResponseDto>>;

public class DismissFailedDungeonRunCommandHandler : IRequestHandler<DismissFailedDungeonRunCommand, Response<DismissFailedDungeonRunResponseDto>>
{
    private readonly IDungeonRunService _dungeonRunService;
    private readonly DungeonHubFactory _dungeonHub;

    public DismissFailedDungeonRunCommandHandler(
        IDungeonRunService dungeonRunService,
        DungeonHubFactory dungeonHub)
    {
        _dungeonRunService = dungeonRunService;
        _dungeonHub = dungeonHub;
    }

    public async Task<Response<DismissFailedDungeonRunResponseDto>> Handle(DismissFailedDungeonRunCommand request, CancellationToken cancellationToken)
    {
        var success = await _dungeonRunService.DismissFailedRunAsync(request.CharacterId, cancellationToken);

        if (!success)
            return Response<DismissFailedDungeonRunResponseDto>.Fail("No failed dungeon run found.");

        return Response<DismissFailedDungeonRunResponseDto>.Success(new DismissFailedDungeonRunResponseDto
        {
            ActiveRun = null,
            Hub = await _dungeonHub.CreateAsync(request.CharacterId, cancellationToken)
        });
    }
}
