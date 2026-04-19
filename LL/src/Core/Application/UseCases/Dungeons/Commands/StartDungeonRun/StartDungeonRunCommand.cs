using Application.Interfaces.Services.LL.Dungeons;
using Application.MediatR.Markers;
using Application.UseCases.Dungeons.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Dungeons.Definitions;
using MediatR;

namespace Application.UseCases.Dungeons.Commands.StartDungeonRun;

public record StartDungeonRunCommand(Guid CharacterId, string DungeonId, DungeonTier DungeonTier) : ICommand<Response<DungeonRunDto>>;

public class StartDungeonRunCommandHandler : IRequestHandler<StartDungeonRunCommand, Response<DungeonRunDto>>
{
    private readonly IMapper _mapper;
    private readonly IDungeonRunService _dungeonRunService;
    public StartDungeonRunCommandHandler(IMapper mapper, IDungeonRunService dungeonRunService)
    {
        _mapper = mapper;
        _dungeonRunService = dungeonRunService;
    }

    public async Task<Response<DungeonRunDto>> Handle(StartDungeonRunCommand request, CancellationToken cancellationToken)
    {
        var dungeon = await _dungeonRunService.StartRunAsync(request.CharacterId, request.DungeonId, cancellationToken);
        
        if (dungeon == null)
            return Response<DungeonRunDto>.Fail("You already have an ongoing dungeon run.");

        var result = _mapper.Map<DungeonRunDto>(dungeon);
        return Response<DungeonRunDto>.Success(result);
    }
}
