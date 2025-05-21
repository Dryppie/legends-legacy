using Application.Interfaces.Services.LL;
using Common.Primitives;
using Domain.Models.Professions.Crafting;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartCraftingAction;
public record StartCraftingActionCommand(Guid CharacterId, string QueueId, string TargetId, CraftingMode Mode) : IRequest<Response<bool>>; 
public class StartCraftingActionCommandHandler : IRequestHandler<StartCraftingActionCommand, Response<bool>>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IActionDetailsService _actionDetailsService;
    public StartCraftingActionCommandHandler(ICharacterActionService characterActionService, IActionDetailsService actionDetailsService)
    {
        _characterActionService = characterActionService;
        _actionDetailsService = actionDetailsService;
    }
    public async Task<Response<bool>> Handle(StartCraftingActionCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.QueueId, out var queueId) ||
            !Guid.TryParse(request.TargetId, out var targetId))
            return Response<bool>.Fail("Unable to start crafting.");

        var characterAction = await _actionDetailsService.CreateCraftingActionDetailsAsync(request.CharacterId, queueId, targetId, request.Mode, cancellationToken);
        if (characterAction == null) return Response<bool>.Fail("Unable to start crafting");

        var success = await _characterActionService.UpdateCraftingCharacterActionAsync(characterAction, cancellationToken);
        return success
            ? Response<bool>.Success(success)
            : Response<bool>.Fail("Unable to start crafting.");
    }
}