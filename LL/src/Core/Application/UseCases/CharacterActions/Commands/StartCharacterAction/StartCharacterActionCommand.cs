using Application.Interfaces.Services.LL;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartCharacterAction;
public record StartCharacterActionCommand(Guid CharacterId, CharacterActionType CharacterActionType, ActionDetails ActionDetails) : IRequest<bool>;

public class StartCharacterActionCommandHandler : IRequestHandler<StartCharacterActionCommand, bool>
{
    private readonly ICharacterActionService _characterActionService;
    public StartCharacterActionCommandHandler(ICharacterActionService characterActionService)
    {
        _characterActionService = characterActionService;
    }
    public async Task<bool> Handle(StartCharacterActionCommand request, CancellationToken cancellationToken)
    {
        var characterAction = new CharacterAction(request.CharacterId, request.CharacterActionType, request.ActionDetails);
        if (characterAction.ActionDetails is CombatActionDetails combatAction)
        {
            combatAction.CharacterTeam = new List<Guid>()
            {
                request.CharacterId,
            };
        }

        return await _characterActionService.StartCharacterActionAsync(characterAction, cancellationToken);
    }
}
