using Application.Authorization.Interfaces;
using Application.Common.Responses;
using Application.Interfaces.Services.LL;
using Application.UseCases.Users.Events;
using Common.Authorization.Security;
using Domain.Models.Users;
using MediatR;

namespace Application.UseCases.Users.Commands.GuestLogin;
public record GuestLoginCommand() : IRequest<Response<Tokens>>;

public class GuestLoginCommandHandler : IRequestHandler<GuestLoginCommand, Response<Tokens>>
{
    private readonly IUserService _userService;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly IMediator _publisher;
    private readonly ICharacterService _characterService;

    public GuestLoginCommandHandler(IUserService userRepository, IJwtGenerator jwtGenerator, IMediator publisher, ICharacterService characterService)
    {
        _userService = userRepository;
        _jwtGenerator = jwtGenerator;
        _publisher = publisher;
        _characterService = characterService;
    }

    public async Task<Response<Tokens>> Handle(GuestLoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Create a new guest user
            var user = await _userService.RegisterGuest();

            await _publisher.Publish(new UserCreatedEvent(user.Id, user.Name), cancellationToken);

            var character = await _characterService.GetMyCharacterAsync(Guid.Parse(user.Id), cancellationToken);
            user.CharacterId = character.Id.ToString();

            // Generate tokens
            var tokens = _jwtGenerator.GenerateTokens(user);

            return Response<Tokens>.Success(tokens);
        }
        catch (Exception)
        {
            return Response<Tokens>.Fail("Token Error");
        }

    }
}