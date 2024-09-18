using Application.Interfaces.Services.LL;
using Domain.Models.CharacterActions;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartCharacterAction;
public record StartCharacterActionCommand(Guid CharacterId, CharacterActionType CharacterActionType, Guid LootTableId) : IRequest<bool>;

public class StartCharacterActionCommandHandler : IRequestHandler<StartCharacterActionCommand, bool>
{
    private readonly ICharacterActionService _characterActionService;
    public StartCharacterActionCommandHandler(ICharacterActionService characterActionService)
    {
        _characterActionService = characterActionService;
    }
    public async Task<bool> Handle(StartCharacterActionCommand request, CancellationToken cancellationToken)
    {
        var characterAction = new CharacterAction(request.CharacterId, request.CharacterActionType, request.LootTableId);
        return await _characterActionService.StartCharacterActionAsync(characterAction, cancellationToken);
    }
}
