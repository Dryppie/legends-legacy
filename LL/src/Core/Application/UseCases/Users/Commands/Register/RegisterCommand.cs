using Application.Interfaces.Services.LL;
using Application.UseCases.Users.Events;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Users.Commands.Register;
public record RegisterCommand(string Username, string Email, string Password) : IRequest<Response<Unit>>;
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Response<Unit>>
{
    private readonly IUserService _userService;
    private readonly IMediator _publisher;

    public RegisterCommandHandler(IUserService userService, IMediator publisher)
    {
        _userService = userService;
        _publisher = publisher;
    }

    public async Task<Response<Unit>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Register the user
            if (request.Username.Length > 26) return Response<Unit>.Fail("Username is too long.");
            var user = await _userService.RegisterAsync(request.Username, request.Email, request.Password, cancellationToken);
            if (user == null) return Response<Unit>.Fail("Email or username is already in use.");

            // Create a character for the registered user
            await _publisher.Publish(new UserCreatedEvent(user.Id, user.Username), cancellationToken);

            return Response<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Response<Unit>.Fail(ex.Message);
        }
    }
}