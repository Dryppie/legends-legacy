using Application.Interfaces.Services.LL.CharacterActions;
using Common.Primitives;
using Domain.Models.CharacterActions;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartCombatAction;
public record StartCombatActionCommand(Guid CharacterId, string AreaId) : IRequest<Response<bool>>;
public class StartCombatActionCommandHandler : IRequestHandler<StartCombatActionCommand, Response<bool>>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IActionDetailsService _actionDetailsService;
    public StartCombatActionCommandHandler(ICharacterActionService characterActionService, IActionDetailsService actionDetailsService)
    {
        _characterActionService = characterActionService;
        _actionDetailsService = actionDetailsService;
    }
    public async Task<Response<bool>> Handle(StartCombatActionCommand request, CancellationToken cancellationToken)
    {
        var combatActionDetails = await _actionDetailsService.CreateCombatActionDetailsAsync(request.AreaId, request.CharacterId, cancellationToken);

        var characterAction = new CharacterAction(request.CharacterId, combatActionDetails);

        var success = await _characterActionService.StartCharacterActionAsync(characterAction, cancellationToken);

        return success
            ? Response<bool>.Success(success)
            : Response<bool>.Fail("Unable to start combat");
    }
}