using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Application.UseCases.Users.Events;
using Common.Authorization.Security;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Users.Commands.Register;

public record RegisterCommand(string Username, string Email, string Password) : ICommand<Response<Tokens>>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Response<Tokens>>
{
    private readonly IUserService _userService;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly ICharacterService _characterService;
    private readonly IMediator _publisher;

    public RegisterCommandHandler(
        IUserService userService,
        IJwtGenerator jwtGenerator,
        ICharacterService characterService,
        IMediator publisher)
    {
        _userService = userService;
        _jwtGenerator = jwtGenerator;
        _characterService = characterService;
        _publisher = publisher;
    }

    public async Task<Response<Tokens>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Username.Length > 26) return Response<Tokens>.Fail("Username is too long.");

            var user = await _userService.RegisterAsync(request.Username, request.Email, request.Password, cancellationToken);
            if (user == null) return Response<Tokens>.Fail("Email or username is already in use.");

            await _publisher.Publish(new UserCreatedEvent(user.Id, user.Username), cancellationToken);

            var character = await _characterService.GetMyCharacterAsync(user.Id, cancellationToken);
            if (character == null) return Response<Tokens>.Fail("Character creation failed during registration.");

            var tokens = await _jwtGenerator.IssueTokens(user, character);
            return Response<Tokens>.Success(tokens);
        }
        catch (Exception ex)
        {
            return Response<Tokens>.Fail(ex.Message);
        }
    }
}
