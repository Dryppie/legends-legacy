using Application.Interfaces.Services.LL;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.CharacterActions.Commands.DeleteCharacterAction;
public record DeleteCharacterActionCommand(Guid CharacterId) : IRequest;

public class DeleteCharacterActionCommandHandler : IRequestHandler<DeleteCharacterActionCommand>
{
    private readonly ICharacterActionService _characterActionService;

    public DeleteCharacterActionCommandHandler(ICharacterActionService characterActionService)
    {
        _characterActionService = characterActionService;
    }

    public async Task Handle(DeleteCharacterActionCommand request, CancellationToken cancellationToken)
    {
        await _characterActionService.DeleteCharacterActionAsync(request.CharacterId, cancellationToken);
    }
}
