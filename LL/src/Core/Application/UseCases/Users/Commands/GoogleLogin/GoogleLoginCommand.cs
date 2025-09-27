using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Application.UseCases.Users.Events;
using Common.Authorization.Security;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Users.Commands.GoogleLogin;
public record GoogleLoginCommand(string IdToken) : ICommand<Response<Tokens>>;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, Response<Tokens>>
{
    private readonly IGoogleAuthService _google;
    private readonly IJwtGenerator _jwt;
    private readonly ICharacterService _characterService;
    private readonly IMediator _publisher;

    public GoogleLoginCommandHandler(IGoogleAuthService google, IJwtGenerator jwt, ICharacterService characterService, IMediator publisher)
    {
        _google = google;
        _jwt = jwt;
        _characterService = characterService;
        _publisher = publisher;
    }

    public async Task<Response<Tokens>> Handle(GoogleLoginCommand req, CancellationToken cancellationToken)
    {
        var googleLoginResult = await _google.LoginOrCreateAsync(req.IdToken, cancellationToken);
        if (googleLoginResult == null) return Response<Tokens>.Fail("Gmail validation failed.");

        var (user, isNew) = googleLoginResult;
        // if brand‑new, create a character
        if (isNew)
            await _publisher.Publish(new UserCreatedEvent(user.Id, user.Username), cancellationToken);

        var character = await _characterService.GetMyCharacterAsync(user.Id, cancellationToken);
        if (character == null) return Response<Tokens>.Fail("Could not find character.");

        var tokens = await _jwt.IssueTokens(user, character);
        return Response<Tokens>.Success(tokens);
    }
}
