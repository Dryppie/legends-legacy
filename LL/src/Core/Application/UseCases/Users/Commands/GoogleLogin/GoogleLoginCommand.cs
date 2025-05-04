using Application.Authorization.Interfaces;
using Application.Common.Responses;
using Common.Authorization.Security;
using MediatR;

namespace Application.UseCases.Users.Commands.GoogleLogin;
public record GoogleLoginCommand(string IdToken) : IRequest<Response<Tokens>>;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, Response<Tokens>>
{
    private readonly IGoogleAuthService _google;
    private readonly IJwtGenerator _jwt;

    public GoogleLoginCommandHandler(IGoogleAuthService google, IJwtGenerator jwt)
    {
        _google = google;
        _jwt = jwt;
    }

    public async Task<Response<Tokens>> Handle(GoogleLoginCommand c, CancellationToken ct)
    {
        try
        {
            var user = await _google.LoginOrCreateAsync(c.IdToken, ct);
            var tokens = _jwt.IssueTokens(user);
            return Response<Tokens>.Success(tokens);
        }
        catch (Exception ex)
        {
            return Response<Tokens>.Fail(ex.Message);
        }
    }
}
