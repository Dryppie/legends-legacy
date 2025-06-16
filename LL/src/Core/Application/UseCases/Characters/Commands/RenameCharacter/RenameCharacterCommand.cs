using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Characters.Commands.RenameCharacter;
public record RenameCharacterCommand(Guid UserId, string NewName) : IRequest<Response<bool>>;
public class RenameCharacterCommandHandler : IRequestHandler<RenameCharacterCommand, Response<bool>>
{
    private readonly ICharacterService _characterService;
    private readonly IUserService _userService;
    public RenameCharacterCommandHandler(ICharacterService characterService, IUserService userService)
    {
        _characterService = characterService;
        _userService = userService;
    }
    public async Task<Response<bool>> Handle(RenameCharacterCommand request, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserInfo(request.UserId, cancellationToken);
        if (user == null || user.IsNameEdited) return Response<bool>.Fail("Character has already been renamed once. No more edits are allowed.");

        var nameUpdateResult = await _characterService.UpdateCharacterNameAsync(request.UserId, request.NewName, cancellationToken);

        if (!nameUpdateResult) return Response<bool>.Fail("Failed to rename character.");

        user.IsNameEdited = true;
        var result = await _userService.UpdateUserInfo(request.UserId, user, cancellationToken);

        return result ? Response<bool>.Success(true) : Response<bool>.Fail("Failed to rename character.");
    }
}
