using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Common.Authorization.Security;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Users.Commands.RenameCharacter;
public record RenameCharacterCommand(Guid UserId, string NewName) : ICommand<Response<Tokens>>;
public class RenameCharacterCommandHandler : IRequestHandler<RenameCharacterCommand, Response<Tokens>>
{
    private readonly ICharacterService _characterService;
    private readonly IUserService _userService;
    private readonly IJwtGenerator _jwtGenerator;
    public RenameCharacterCommandHandler(ICharacterService characterService, IUserService userService, IJwtGenerator jwtGenerator)
    {
        _characterService = characterService;
        _userService = userService;
        _jwtGenerator = jwtGenerator;
    }
    public async Task<Response<Tokens>> Handle(RenameCharacterCommand request, CancellationToken cancellationToken)
    {
        if (request.NewName.Length > 26) return Response<Tokens>.Fail("Username is too long.");

        var user = await _userService.GetUserById(request.UserId, cancellationToken);
        if (user == null || user.IsNameEdited) return Response<Tokens>.Fail("Character has already been renamed once. No more edits are allowed.");

        var character = await _characterService.UpdateCharacterNameAsync(request.UserId, request.NewName, cancellationToken);

        if (character == null) return Response<Tokens>.Fail("Failed to rename character.");

        var result = _userService.UpdateUserInfo(user);
        if (!result) return Response<Tokens>.Fail("Failed to rename character.");

        var tokens = await _jwtGenerator.IssueTokens(user, character);
        return Response<Tokens>.Success(tokens);
    }
}
