using Application.Interfaces.Services.LL;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.GatheringNodes;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartGatheringAction;
public record StartGatheringActionCommand(Guid CharacterId, CharacterActionType CharacterActionType, string GatheringNodeId, GatheringType GatheringType) : IRequest<bool>;
public class StartGatheringActionCommandHandler : IRequestHandler<StartGatheringActionCommand, bool>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IActionDetailsService _actionDetailsService;
    public StartGatheringActionCommandHandler(ICharacterActionService characterActionService, IActionDetailsService actionDetailsService)
    {
        _characterActionService = characterActionService;
        _actionDetailsService = actionDetailsService;
    }
    public async Task<bool> Handle(StartGatheringActionCommand request, CancellationToken cancellationToken)
    {
        var gatheringActionDetails = await _actionDetailsService
            .CreateGatheringActionDetailsAsync(request.GatheringNodeId, request.GatheringType, request.CharacterId, cancellationToken);

        var characterAction = new CharacterAction(request.CharacterId, request.CharacterActionType, gatheringActionDetails);

        if (characterAction.ActionDetails is GatheringActionDetails gatheringAction)
        {
            gatheringAction.LootTable = null;
        }

        return await _characterActionService.StartCharacterActionAsync(characterAction, cancellationToken);
    }
}
