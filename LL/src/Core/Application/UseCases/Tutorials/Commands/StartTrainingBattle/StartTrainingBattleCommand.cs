using Application.Interfaces.Services.LL.Tutorials;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Tutorials.Commands.StartTrainingBattle;

public sealed record StartTrainingBattleCommand(Guid CharacterId) : ICommand<Response<CombatResultDto>>;

public sealed class StartTrainingBattleCommandHandler
    : IRequestHandler<StartTrainingBattleCommand, Response<CombatResultDto>>
{
    private readonly ITutorialBattleService _tutorialBattleService;
    private readonly IMapper _mapper;

    public StartTrainingBattleCommandHandler(
        ITutorialBattleService tutorialBattleService,
        IMapper mapper)
    {
        _tutorialBattleService = tutorialBattleService;
        _mapper = mapper;
    }

    public async Task<Response<CombatResultDto>> Handle(
        StartTrainingBattleCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _tutorialBattleService.StartTrainingBattleAsync(
            request.CharacterId,
            cancellationToken);

        return result is null
            ? Response<CombatResultDto>.Fail("Unable to start training battle.")
            : Response<CombatResultDto>.Success(_mapper.Map<CombatResultDto>(result));
    }
}
