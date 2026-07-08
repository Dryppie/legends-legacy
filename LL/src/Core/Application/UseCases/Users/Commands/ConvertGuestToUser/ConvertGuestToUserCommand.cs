using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Application.UseCases.Users.Events;
using Application.UseCases.Users;
using Common.Authorization.Security;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Users.Commands.ConvertGuestToUser;
public record ConvertGuestToUserCommand(Guid UserId, string CharacterName, string Email, string Password) : ICommand<Response<Tokens>>;

public class ConvertGuestToUserCommandHandler : IRequestHandler<ConvertGuestToUserCommand, Response<Tokens>>
{
    private readonly IUserService _userService;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly IMediator _publisher;
    private readonly ICharacterService _characterService;

    public ConvertGuestToUserCommandHandler(IUserService userRepository, IJwtGenerator jwtGenerator, IMediator publisher, ICharacterService characterService)
    {
        _userService = userRepository;
        _jwtGenerator = jwtGenerator;
        _publisher = publisher;
        _characterService = characterService;
    }

    public async Task<Response<Tokens>> Handle(ConvertGuestToUserCommand request, CancellationToken cancellationToken)
    {
        if (!AuthInputValidator.TryValidateRegistration(
                request.CharacterName,
                request.Email,
                request.Password,
                out var input,
                out var validationError))
        {
            return Response<Tokens>.Fail(validationError);
        }

        var character = await _characterService.GetMyCharacterAsync(request.UserId, cancellationToken);
        if (character == null) return Response<Tokens>.Fail("No character is bound to this account.");

        if (await _characterService.IsCharacterNameTakenAsync(input.CharacterName, character.Id, cancellationToken))
        {
            return Response<Tokens>.Fail("Character name is already in use.");
        }

        var user = await _userService.ConvertGuestToUser(request.UserId, input.Email, input.Password, cancellationToken);
        if (user == null) return Response<Tokens>.Fail("Account is already registered or the email is already in use.");

        await _publisher.Publish(new ConvertedGuestToUserEvent(user.Id, input.CharacterName), cancellationToken);


        var tokens = await _jwtGenerator.IssueTokens(user, character);

        return Response<Tokens>.Success(tokens);
    }
}
