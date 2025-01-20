using Application.Interfaces.Services.LL;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartGatheringAction;
public record StartGatheringActionCommand(Guid CharacterId, CharacterActionType CharacterActionType, ActionDetails ActionDetails) : IRequest<bool>;
public class StartGatheringActionCommandHandler : IRequestHandler<StartGatheringActionCommand, bool>
{
    private readonly ICharacterActionService _characterActionService;
    public StartGatheringActionCommandHandler(ICharacterActionService characterActionService)
    {
        _characterActionService = characterActionService;
    }
    public async Task<bool> Handle(StartGatheringActionCommand request, CancellationToken cancellationToken)
    {
        var characterAction = new CharacterAction(request.CharacterId, request.CharacterActionType, request.ActionDetails);

        if (characterAction.ActionDetails is GatheringActionDetails gatheringAction)
        {
            gatheringAction.LootTable = null;
        }

        return await _characterActionService.StartCharacterActionAsync(characterAction, cancellationToken);
    }
}
