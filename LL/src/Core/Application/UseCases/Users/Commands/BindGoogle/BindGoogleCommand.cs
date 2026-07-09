using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Common.Authorization.Security;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Users.Commands.BindGoogle;
public record BindGoogleCommand(Guid UserId, string IdToken) : ICommand<Response<Tokens>>;

public class BindGoogleCommandHandler
    : IRequestHandler<BindGoogleCommand, Response<Tokens>>
{
    private readonly IGoogleAuthService _google;
    private readonly IJwtGenerator _jwt;
    private readonly ICharacterService _characterService;

    public BindGoogleCommandHandler(IGoogleAuthService google, IJwtGenerator jwt, ICharacterService characterService)
    {
        _google = google;
        _jwt = jwt;
        _characterService = characterService;
    }

    public async Task<Response<Tokens>> Handle(BindGoogleCommand c, CancellationToken ct)
    {
        var bindResult = await _google.BindAsync(c.UserId, c.IdToken, ct);
        if (bindResult is null)
        {
            return Response<Tokens>.Fail("This Gmail is already bound to a different account.");
        }

        var character = await _characterService.GetMyCharacterAsync(bindResult.User.Id, ct);
        if (character is null)
        {
            return Response<Tokens>.Fail("No character is bound to this account.");
        }

        var tokens = await _jwt.IssueTokens(bindResult.User, character);
        return Response<Tokens>.Success(tokens);
    }
}
