using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Application.UseCases.Users.Events;
using Common.Authorization.Security;
using MediatR;

namespace Application.UseCases.Users.Commands.Register;
public record RegisterCommand(string Username, string Email, string Password) : IRequest<bool>;

internal class RegisterCommandHandler : IRequestHandler<RegisterCommand, bool>
{
    private readonly IUserService _userService;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly IMediator _mediator;

    public RegisterCommandHandler(IUserService userService, IJwtGenerator jwtGenerator, IMediator mediator)
    {
        _userService = userService;
        _jwtGenerator = jwtGenerator;
        _mediator = mediator;
    }

    public async Task<bool> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Register the user
            var user = await _userService.Register(request.Username, request.Email, request.Password);

            // Create a character for the registered user
            var test = _mediator.Publish(new UserCreatedEvent(user.Id, user.Name), cancellationToken);

            return test.IsCompleted;
        }
        catch
        {
#if !DEBUG
            throw new Exception($"Problem creating user {request.Username}");
#endif
            return false;
        }
    }
}