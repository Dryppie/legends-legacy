using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Application.UseCases.Users.Events;
using Domain.Models.Users;
using MediatR;

namespace Application.UseCases.Users.Commands.ConvertGuestToUser;
public record ConvertGuestToUserCommand(string UserId, string Username, string Email, string Password) : IRequest<bool>;

public class ConvertGuestToUserCommandHandler : IRequestHandler<ConvertGuestToUserCommand, bool>
{
    private readonly IUserService _userService;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly IMediator _publisher;

    public ConvertGuestToUserCommandHandler(IUserService userRepository, IJwtGenerator jwtGenerator, IMediator publisher)
    {
        _userService = userRepository;
        _jwtGenerator = jwtGenerator;
        _publisher = publisher;
    }

    public async Task<bool> Handle(ConvertGuestToUserCommand request, CancellationToken cancellationToken)
    {
        // TODO: Ensure the username isn't already taken
        var user = await _userService.ConvertGuestToUser(request.UserId, request.Username, request.Email, request.Password);

        // TODO: Change character name after the user has changed theirs
        await _publisher.Publish(new ConvertedGuestToUserEvent(user.Id, user.Name));

        return true;
    }
}
