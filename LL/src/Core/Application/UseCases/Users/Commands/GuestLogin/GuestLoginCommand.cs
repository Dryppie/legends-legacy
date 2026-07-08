using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Application.UseCases.Users.Events;
using Common.Authorization.Security;
using Common.Primitives;
using Domain.Models.Users;
using MediatR;

namespace Application.UseCases.Users.Commands.GuestLogin;
public record GuestLoginCommand() : ICommand<Response<Tokens>>;
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
            AppUser? user = null;
            var characterName = string.Empty;

            for (var attempt = 0; attempt < 10; attempt++)
            {
                characterName = GuestCharacterNameGenerator.Generate();
                if (await _characterService.IsCharacterNameTakenAsync(characterName, null, cancellationToken))
                {
                    continue;
                }

                user = await _userService.RegisterGuestAsync(characterName, cancellationToken);
                if (user is not null) break;
            }

            if (user == null) return Response<Tokens>.Fail("Failed to register guest");

            await _publisher.Publish(new UserCreatedEvent(user.Id, characterName), cancellationToken);

            var character = await _characterService.GetMyCharacterAsync(user.Id, cancellationToken);
            if (character == null) return Response<Tokens>.Fail("Character creation failed during guest registration");

            // Generate tokens
            var tokens = await _jwtGenerator.IssueTokens(user, character);

            return Response<Tokens>.Success(tokens);
        }
        catch (Exception)
        {
            return Response<Tokens>.Fail("Token Error");
        }

    }
}
