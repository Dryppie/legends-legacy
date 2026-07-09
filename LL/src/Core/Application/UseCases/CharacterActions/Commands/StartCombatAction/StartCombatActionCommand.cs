using Application.Interfaces.Services.LL.CharacterActions;
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
    private readonly IMapper _mapper;

    public StartCombatActionCommandHandler(
        ICharacterActionService characterActionService,
        IActionDetailsService actionDetailsService,
        IMapper mapper)
    {
        _characterActionService = characterActionService;
        _actionDetailsService = actionDetailsService;
        _mapper = mapper;
    }

    public async Task<Response<CharacterActionDto>> Handle(StartCombatActionCommand request, CancellationToken cancellationToken)
    {
        var combatActionDetails = await _actionDetailsService.CreateCombatActionDetailsAsync(request.AreaId, request.CharacterId, cancellationToken);
        if (combatActionDetails == null)
            return Response<CharacterActionDto>.Fail("Unable to start combat.");

        var characterAction = new CharacterAction(request.CharacterId, combatActionDetails);

        var startedAction = await _characterActionService.StartCharacterActionAsync(characterAction, cancellationToken);

        return startedAction is not null
            ? Response<CharacterActionDto>.Success(_mapper.Map<CharacterActionDto>(startedAction))
            : Response<CharacterActionDto>.Fail("Unable to start combat");
    }
}
