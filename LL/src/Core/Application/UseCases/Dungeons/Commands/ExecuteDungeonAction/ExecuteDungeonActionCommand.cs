using Application.Interfaces.Services.LL.Dungeons;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Application.UseCases.Dungeons.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Dungeons.Commands.ExecuteDungeonAction;

public record ExecuteDungeonActionCommand(Guid RunId, string ActionId, object? Payload) : ICommand<Response<ExecuteDungeonActionResponseDto>>;
public class ExecuteDungeonActionCommandHandler : IRequestHandler<ExecuteDungeonActionCommand, Response<ExecuteDungeonActionResponseDto>>
{
    private readonly IDungeonRunService _dungeonRunService;
    private readonly IMapper _mapper;

    public ExecuteDungeonActionCommandHandler(
        IDungeonRunService dungeonRunService,
        IMapper mapper)
    {
        _dungeonRunService = dungeonRunService;
        _mapper = mapper;
    }

    public async Task<Response<ExecuteDungeonActionResponseDto>> Handle(
        ExecuteDungeonActionCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _dungeonRunService.ExecuteActionAsync(
            request.RunId,
            request.ActionId,
            request.Payload,
            cancellationToken);

        if (result == null)
            return Response<ExecuteDungeonActionResponseDto>.Fail("Failed to execute action on dungeon run.");

        var response = new ExecuteDungeonActionResponseDto
        {
            Run = _mapper.Map<DungeonRunDto>(result.Run),
            Outcome = _mapper.Map<DungeonActionOutcomeDto>(result.Outcome),
            CombatSession = result.CombatSession is null ? null : _mapper.Map<CombatSessionDto>(result.CombatSession),
            Message = result.Message
        };

        return Response<ExecuteDungeonActionResponseDto>.Success(response);
    }
}