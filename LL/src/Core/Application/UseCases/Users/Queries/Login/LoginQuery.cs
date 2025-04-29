using Application.Authorization.Interfaces;
using Application.Common.Responses;
using Application.Interfaces.Services.LL;
using Application.UseCases.Characters.Dtos;
using Common.Authorization.Security;
using Common.Exceptions;
using MediatR;

namespace Application.UseCases.Users.Queries.Login;
public record LoginQuery(string Email, string Password) : IRequest<Response<Tokens>>;

public class LoginQueryHandler : IRequestHandler<LoginQuery, Response<Tokens>>
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

    public async Task<Response<Tokens>> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.Login(request.Email, request.Password);
            var character = await _characterService.GetMyCharacterAsync(Guid.Parse(user.Id), cancellationToken);

            user.CharacterId = character.Id.ToString();

            var tokens = _jwtGenerator.GenerateTokens(user);
            return Response<Tokens>.Success(tokens);
        }
        catch
        {
            return Response<Tokens>.Fail("Token Error");
        }
    }
}