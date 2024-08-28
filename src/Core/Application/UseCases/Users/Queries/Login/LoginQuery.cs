using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Common.Authorization.Security;
using Common.Exceptions;
using MediatR;

namespace Application.UseCases.Users.Queries.Login;
public record LoginQuery(string Email, string Password) : IRequest<Tokens>;

public class LoginQueryHandler : IRequestHandler<LoginQuery, Tokens>
{
    private readonly IUserService _userService;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly ICharacterService _characterService;

    public LoginQueryHandler(IUserService userService, IJwtGenerator jwtGenerator, ICharacterService characterService)
    {
        _userService = userService;
        _jwtGenerator = jwtGenerator;
        _characterService = characterService;
    }

    public async Task<Tokens> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.Login(request.Email, request.Password);
            var character = await _characterService.GetMyCharacterAsync(Guid.Parse(user.Id));

            user.CharacterId = character.Id.ToString();

            return _jwtGenerator.GenerateTokens(user);
        }
        catch
        {
            throw new NotFoundException();
        }
    }
}