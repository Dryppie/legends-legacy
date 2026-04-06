using Application.Interfaces.Services.LL.Dungeons;
using Application.MediatR.Markers;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Dungeons.Runs;
using MediatR;

namespace Application.UseCases.Dungeons.Commands.StartDungeonRun;

public record StartDungeonRunCommand(Guid CharacterId, string DungeonId) : ICommand<Response<DungeonRun>>;

public class StartDungeonRunCommandHandler : IRequestHandler<StartDungeonRunCommand, Response<DungeonRun>>
{
    private readonly IMapper _mapper;
    private readonly IDungeonRunService _dungeonRunService;
    public StartDungeonRunCommandHandler(IMapper mapper, IDungeonRunService dungeonRunService)
    {
        _mapper = mapper;
        _dungeonRunService = dungeonRunService;
    }

    public async Task<Response<DungeonRun>> Handle(StartDungeonRunCommand request, CancellationToken cancellationToken)
    {
        // Implementation to start a dungeon run goes here.

        var dungeon = await _dungeonRunService.StartRunAsync(request.CharacterId, request.DungeonId, cancellationToken);
        
        return Response<DungeonRun>.Success(dungeon);
    }
}
