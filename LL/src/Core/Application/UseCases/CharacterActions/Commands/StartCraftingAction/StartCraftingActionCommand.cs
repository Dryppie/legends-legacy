using Application.Interfaces.Services.LL.CharacterActions;
using Common.Primitives;
using Domain.Models.Professions.Crafting;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartCraftingAction;
public record StartCraftingActionCommand(Guid CharacterId, string QueueId, string ItemInstanceId) : IRequest<Response<bool>>; 
public class StartCraftingActionCommandHandler : IRequestHandler<StartCraftingActionCommand, Response<bool>>
{
    private readonly ICharacterActionService _characterActionService;
    public StartCraftingActionCommandHandler(ICharacterActionService characterActionService)
    {
        _characterActionService = characterActionService;
    }
    public async Task<Response<bool>> Handle(StartCraftingActionCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.QueueId, out var queueId) ||
            !Guid.TryParse(request.ItemInstanceId, out var itemInstanceId))
            return Response<bool>.Fail("Unable to start crafting.");

        var queueItem = new CraftingQueueItem
        {
            Id = queueId,
            EquipmentInstanceId = itemInstanceId
        };

        var success = await _characterActionService.UpdateCraftingCharacterActionAsync(request.CharacterId, queueItem, cancellationToken);
        return success
            ? Response<bool>.Success(success)
            : Response<bool>.Fail("Unable to start crafting.");
    }
}