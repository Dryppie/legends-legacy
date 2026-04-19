using Application.Interfaces.Services.LL.Dungeons;
using Application.MediatR.Markers;
using Application.UseCases.Dungeons.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Dungeons.Commands.ExecuteDungeonAction;

public record ExecuteDungeonActionCommand(Guid RunId, string ActionId, object? Payload) : ICommand<Response<DungeonRunDto>>;
public class ExecuteDungeonActionCommandHandler : IRequestHandler<ExecuteDungeonActionCommand, Response<DungeonRunDto>>
{
    private readonly IDungeonRunService _dungeonRunService;
    private readonly IMapper _mapper;
    public ExecuteDungeonActionCommandHandler(IDungeonRunService dungeonRunService, IMapper mapper)
    {
        _dungeonRunService = dungeonRunService;
        _mapper = mapper;
    }

    public async Task<Response<DungeonRunDto>> Handle(ExecuteDungeonActionCommand request, CancellationToken cancellationToken)
    {
        var dungeonRun = await _dungeonRunService.ExecuteAction(request.RunId, request.ActionId, request.Payload, cancellationToken);
        if (dungeonRun == null) return Response<DungeonRunDto>.Fail("Failed to execute action on dungeon run.");

        var dungeonRunDto = _mapper.Map<DungeonRunDto>(dungeonRun);
        return Response<DungeonRunDto>.Success(dungeonRunDto);
    }
}