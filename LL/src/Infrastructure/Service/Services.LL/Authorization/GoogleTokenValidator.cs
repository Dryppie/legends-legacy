using Application.Authorization.Interfaces;
using Google.Apis.Auth;
using Google.Apis.Util;
using Microsoft.Extensions.Configuration;

namespace Services.LL.Authorization;
public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly string _clientId;
    private readonly IClock _clock = SystemClock.Default;

    public GoogleTokenValidator(IConfiguration cfg)
    {
        _clientId = cfg["Google:ClientId"] ??
                    throw new InvalidOperationException("Google:ClientId missing");
    }

    public Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { _clientId },
            Clock = _clock
        };
        return GoogleJsonWebSignature.ValidateAsync(idToken, _clock, false);
    }
}