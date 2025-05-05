using Application.Authorization.Interfaces;
using Application.Common.Responses;
using Application.Interfaces.Services.LL;
using Application.UseCases.Users.Events;
using Common.Authorization.Security;
using MediatR;

namespace Application.UseCases.Users.Commands.ConvertGuestToUser;
public record ConvertGuestToUserCommand(Guid UserId, string Username, string Email, string Password) : IRequest<Response<Tokens>>;

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
        var user = await _userService.ConvertGuestToUser(request.UserId, request.Username, request.Email, request.Password, cancellationToken);

        var character = await _characterService.GetMyCharacterAsync(user.Id, cancellationToken);
        user.CharacterId = character.Id;
        // TODO: Change character name after the user has changed theirs
        await _publisher.Publish(new ConvertedGuestToUserEvent(user.Id, user.Username!));


        var tokens = _jwtGenerator.IssueTokens(user);

        return Response<Tokens>.Success(tokens);
    }
}
