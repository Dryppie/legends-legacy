using Application.Interfaces.Services.LL.CharacterActions;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.DeleteCharacterAction;
public record DeleteCharacterActionCommand(Guid CharacterId) : ICommand<Response<bool>>;

public class DeleteCharacterActionCommandHandler : IRequestHandler<DeleteCharacterActionCommand, Response<bool>>
{
    private readonly ICharacterActionService _characterActionService;

    public DeleteCharacterActionCommandHandler(ICharacterActionService characterActionService)
    {
        _characterActionService = characterActionService;
    }

    public async Task<Response<bool>> Handle(DeleteCharacterActionCommand request, CancellationToken cancellationToken)
    {
        var success = await _characterActionService.DeleteCharacterActionAsync(request.CharacterId, cancellationToken);
        
        return success
            ? Response<bool>.Success(success)
            : Response<bool>.Fail("No action to delete.");
    }
}
