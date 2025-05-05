using System;
using Application.Authorization.Interfaces;
using Application.Common.Responses;
using Application.Interfaces.Services.LL;
using Application.UseCases.Users.Events;
using Common.Authorization.Security;
using MediatR;

namespace Application.UseCases.Users.Commands.GoogleLogin;
public record GoogleLoginCommand(string IdToken) : IRequest<Response<Tokens>>;

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

    public async Task<Response<Tokens>> Handle(GoogleLoginCommand req, CancellationToken ct)
    {
        try
        {
            var (user, isNew) = await _google.LoginOrCreateAsync(req.IdToken, ct);

            // if brand‑new, create a character
            if (isNew)
            {
                await _publisher.Publish(
                    new UserCreatedEvent(user.Id, user.Username ?? ""), ct);
            }

            // always make sure the CharacterId is on the user before we mint JWT
            var character = await _characterService.GetMyCharacterAsync(user.Id, ct);
            user.CharacterId = character.Id;

            var tokens = _jwt.IssueTokens(user);
            return Response<Tokens>.Success(tokens);
        }
        catch (Exception ex)
        {
            return Response<Tokens>.Fail(ex.Message);
        }
    }
}
