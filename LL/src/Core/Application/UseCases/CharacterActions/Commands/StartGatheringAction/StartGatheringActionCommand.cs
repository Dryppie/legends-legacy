using Application.Interfaces.Services.LL.CharacterActions;
using Common.Primitives;
using Domain.Models.CharacterActions;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartGatheringAction;
public record StartGatheringActionCommand(Guid CharacterId, string GatheringNodeId) : IRequest<Response<bool>>;
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
        var gatheringActionDetails = await _actionDetailsService
            .CreateGatheringActionDetailsAsync(request.GatheringNodeId, request.CharacterId, cancellationToken);

        if (gatheringActionDetails == null) return Response<bool>.Fail("Unable to start gathering");

        gatheringActionDetails.LootTable = null;

        var characterAction = new CharacterAction(request.CharacterId, gatheringActionDetails);

        var success = await _characterActionService.StartCharacterActionAsync(characterAction, cancellationToken);

        return success
            ? Response<bool>.Success(success)
            : Response<bool>.Fail("Unable to start gathering");
    }
}
