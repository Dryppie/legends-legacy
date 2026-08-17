using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Administration;
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
    private readonly IAccountAccessPolicy _accountAccess;

    public GoogleLoginCommandHandler(
        IGoogleAuthService google,
        IJwtGenerator jwt,
        ICharacterService characterService,
        IMediator publisher,
        IAccountAccessPolicy accountAccess)
    {
        _google = google;
        _jwt = jwt;
        _characterService = characterService;
        _publisher = publisher;
        _accountAccess = accountAccess;
    }

    public async Task<Response<Tokens>> Handle(GoogleLoginCommand req, CancellationToken cancellationToken)
    {
        var googleLoginResult = await _google.LoginOrCreateAsync(req.IdToken, cancellationToken);
        if (googleLoginResult == null) return Response<Tokens>.Fail("Gmail validation failed.");

        var (user, isNew, characterName) = googleLoginResult;
        if (await _accountAccess.GetActiveBanAsync(user.Id, cancellationToken) is not null)
            return Response<Tokens>.Fail("This account is suspended.");
        // if brand‑new, create a character
        if (isNew)
            await _publisher.Publish(new UserCreatedEvent(user.Id, characterName ?? user.Username), cancellationToken);

        var character = await _characterService.GetMyCharacterAsync(user.Id, cancellationToken);
        if (character == null) return Response<Tokens>.Fail("Could not find character.");

        var tokens = await _jwt.IssueTokens(user, character);
        return Response<Tokens>.Success(tokens);
    }
}
