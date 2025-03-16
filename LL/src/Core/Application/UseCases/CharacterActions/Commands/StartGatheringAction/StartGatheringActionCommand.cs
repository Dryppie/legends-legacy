using Application.Common.Responses;
using Application.Interfaces.Services.LL;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.GatheringNodes;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartGatheringAction;
public record StartGatheringActionCommand(Guid CharacterId, string GatheringNodeId, GatheringType GatheringType) : IRequest<Response<bool>>;
public class StartGatheringActionCommandHandler : IRequestHandler<StartGatheringActionCommand, Response<bool>>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IActionDetailsService _actionDetailsService;
    public StartGatheringActionCommandHandler(ICharacterActionService characterActionService, IActionDetailsService actionDetailsService)
    {
        _characterActionService = characterActionService;
        _actionDetailsService = actionDetailsService;
    }
    public async Task<Response<bool>> Handle(StartGatheringActionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var gatheringActionDetails = await _actionDetailsService
                        .CreateGatheringActionDetailsAsync(request.GatheringNodeId, request.GatheringType, request.CharacterId, cancellationToken);

            var characterAction = new CharacterAction(request.CharacterId, gatheringActionDetails);

            if (characterAction.ActionDetails is GatheringActionDetails gatheringAction)
            {
                gatheringAction.LootTable = null;
            }

            var startCharacterGathering = await _characterActionService.StartCharacterActionAsync(characterAction, cancellationToken);
            return Response<bool>.Success(startCharacterGathering);
        }
        catch (Exception)
        {
            return Response<bool>.Fail("Error starting gathering for: " + request.CharacterId + "gathering material: " + request.GatheringType + request.GatheringNodeId);
        }
        
    }
}
