using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Common.Authorization.Security;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Users.Commands.Login;
public record LoginCommand(string Email, string Password) : ICommand<Response<Tokens>>;
public class LoginCommandHandler : IRequestHandler<LoginCommand, Response<Tokens>>
{
    private readonly IUserService _userService;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly ICharacterService _characterService;

    public LoginCommandHandler(IUserService userService, IJwtGenerator jwtGenerator, ICharacterService characterService)
    {
        _userService = userService;
        _jwtGenerator = jwtGenerator;
        _characterService = characterService;
    }

    public async Task<Response<Tokens>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (user == null) return Response<Tokens>.Fail("Login error. Check your credentials.");

        var character = await _characterService.GetMyCharacterAsync(user.Id, cancellationToken);
        if (character == null) return Response<Tokens>.Fail("No character exists with this account.");

        var tokens = await _jwtGenerator.IssueTokens(user, character);
        return Response<Tokens>.Success(tokens);
    }
}