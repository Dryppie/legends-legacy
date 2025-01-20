using Application.Interfaces.Services.LL;
using Domain.Models.CharacterActions;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartCombatAction;
public record StartCombatActionCommand(Guid CharacterId, CharacterActionType CharacterActionType, string AreaName) : IRequest<bool>;
public class StartCombatActionCommandHandler : IRequestHandler<StartCombatActionCommand, bool>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IActionDetailsService _actionDetailsService;
    public StartCombatActionCommandHandler(ICharacterActionService characterActionService, IActionDetailsService actionDetailsService)
    {
        _characterActionService = characterActionService;
        _actionDetailsService = actionDetailsService;
    }
    public async Task<bool> Handle(StartCombatActionCommand request, CancellationToken cancellationToken)
    {
        var combatActionDetails = await _actionDetailsService.CreateCombatActionDetailsFromAreaNameAsync(request.AreaName, request.CharacterId, cancellationToken);

        var characterAction = new CharacterAction(request.CharacterId, request.CharacterActionType, combatActionDetails);

        return await _characterActionService.StartCharacterActionAsync(characterAction, cancellationToken);
    }
}