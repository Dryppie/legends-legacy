using Application.Common.Responses;
using Application.Interfaces.Services.LL;
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
        try
        {
            var combatActionDetails = await _actionDetailsService.CreateCombatActionDetailsAsync(request.AreaId, request.CharacterId, cancellationToken);

            var characterAction = new CharacterAction(request.CharacterId, combatActionDetails);

            var characterActionStart = await _characterActionService.StartCharacterActionAsync(characterAction, cancellationToken);

            return Response<bool>.Success(characterActionStart);
        }
        catch (Exception)
        {
            return Response<bool>.Fail("Error starting combat action for: " +  request.CharacterId + "in area: " + request.AreaId);
        }
        
    }
}