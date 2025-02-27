using Application.Authorization.Interfaces;
using Application.Common.Responses;
using Application.Interfaces.Services.LL;
using Application.UseCases.Users.Events;
using MediatR;

namespace Application.UseCases.Users.Commands.Register;
public record RegisterCommand(string Username, string Email, string Password) : IRequest<Response<Unit>>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Response<Unit>>
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

    public async Task<Response<Unit>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Register the user
            var user = await _userService.Register(request.Username, request.Email, request.Password);

            // Create a character for the registered user
            await _publisher.Publish(new UserCreatedEvent(user.Id, user.Name), cancellationToken);

            return Response<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Response<Unit>.Fail(ex.Message);
        }
    }
}