using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Tutorials;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using Domain.Models.CharacterActions;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartCombatAction;
public record StartCombatActionCommand(Guid CharacterId, string AreaId) : ICommand<Response<CharacterActionDto>>;
public class StartCombatActionCommandHandler : IRequestHandler<StartCombatActionCommand, Response<CharacterActionDto>>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IActionDetailsService _actionDetailsService;
    private readonly ITutorialService _tutorialService;
    private readonly ITutorialProgressionService _tutorialProgression;
    private readonly IMapper _mapper;

    public StartCombatActionCommandHandler(
        ICharacterActionService characterActionService,
        IActionDetailsService actionDetailsService,
        ITutorialService tutorialService,
        ITutorialProgressionService tutorialProgression,
        IMapper mapper)
    {
        _characterActionService = characterActionService;
        _actionDetailsService = actionDetailsService;
        _tutorialService = tutorialService;
        _tutorialProgression = tutorialProgression;
        _mapper = mapper;
    }

    public async Task<Response<CharacterActionDto>> Handle(StartCombatActionCommand request, CancellationToken cancellationToken)
    {
        if (!await _tutorialService.CanStartCombatAreaAsync(
                request.CharacterId,
                request.AreaId,
                cancellationToken))
        {
            return Response<CharacterActionDto>.Fail(
                "Complete your current First Steps objective before starting combat here.");
        }

        var combatActionDetails = await _actionDetailsService.CreateCombatActionDetailsAsync(request.AreaId, request.CharacterId, cancellationToken);
        if (combatActionDetails == null)
            return Response<CharacterActionDto>.Fail("Unable to start combat.");

        var characterAction = new CharacterAction(request.CharacterId, combatActionDetails);

        var startedAction = await _characterActionService.StartCharacterActionAsync(characterAction, cancellationToken);
        if (startedAction is not null)
        {
            await _tutorialProgression.TryProgressAsync(
                request.CharacterId,
                TutorialTrigger.CombatActionStarted(request.AreaId),
                cancellationToken);
        }

        return startedAction is not null
            ? Response<CharacterActionDto>.Success(_mapper.Map<CharacterActionDto>(startedAction))
            : Response<CharacterActionDto>.Fail("Unable to start combat");
    }
}
