using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Application.UseCases.Users.Events;
using Common.Authorization.Security;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Users.Commands.ConvertGuestToUser;
public record ConvertGuestToUserCommand(Guid UserId, string Username, string Email, string Password) : ICommand<Response<Tokens>>;

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
        if (request.Username.Length > 26) return Response<Tokens>.Fail("Username is too long.");

        var user = await _userService.ConvertGuestToUser(request.UserId, request.Username, request.Email, request.Password, cancellationToken);
        if (user == null) return Response<Tokens>.Fail("Username or email might already be in use.");

        var character = await _characterService.GetMyCharacterAsync(user.Id, cancellationToken);
        if (character == null) return Response<Tokens>.Fail("No character is bound to this account.");

        await _publisher.Publish(new ConvertedGuestToUserEvent(user.Id, user.Username), cancellationToken);


        var tokens = await _jwtGenerator.IssueTokens(user, character);

        return Response<Tokens>.Success(tokens);
    }
}
