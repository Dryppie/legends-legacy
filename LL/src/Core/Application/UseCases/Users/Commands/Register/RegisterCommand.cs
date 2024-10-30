using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Application.UseCases.Users.Events;
using MediatR;

namespace Application.UseCases.Users.Commands.Register;
public record RegisterCommand(string Username, string Email, string Password) : IRequest;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand>
{
    private readonly IUserService _userService;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly IMediator _publisher;

    public RegisterCommandHandler(IUserService userService, IJwtGenerator jwtGenerator, IMediator publisher)
    {
        _userService = userService;
        _jwtGenerator = jwtGenerator;
        _publisher = publisher;
    }

    public async Task Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Register the user
            var user = await _userService.Register(request.Username, request.Email, request.Password);

            // Create a character for the registered user
            await _publisher.Publish(new UserCreatedEvent(user.Id, user.Name), cancellationToken);

            
        }
        catch
        {
            throw new Exception($"Problem creating user {request.Username}");
        }
    }
}