using Application.Common.Responses;
using Application.Interfaces.Services.LL;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.CharacterActions.Commands.DeleteCharacterAction;
public record DeleteCharacterActionCommand(Guid CharacterId) : IRequest<Response<Unit>>;

public class DeleteCharacterActionCommandHandler : IRequestHandler<DeleteCharacterActionCommand, Response<Unit>>
{
    private readonly ICharacterActionService _characterActionService;

    public DeleteCharacterActionCommandHandler(ICharacterActionService characterActionService)
    {
        _characterActionService = characterActionService;
    }

    public async Task<Response<Unit>> Handle(DeleteCharacterActionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _characterActionService.DeleteCharacterActionAsync(request.CharacterId, cancellationToken);
            return await Task.FromResult(Response<Unit>.Success(Unit.Value));
        }
        catch (Exception)
        {
            return await Task.FromResult(Response<Unit>.Fail("Error deleting character: " + request.CharacterId));
        }
        
    }
}
