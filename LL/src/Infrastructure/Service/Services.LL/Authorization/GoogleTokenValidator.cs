using Application.Authorization.Interfaces;
using Common.Options;
using Google.Apis.Auth;
using Google.Apis.Util;
using Microsoft.Extensions.Options;

namespace Services.LL.Authorization;
public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly string _clientId;
    private readonly IClock _clock = SystemClock.Default;

    public GoogleTokenValidator(IOptions<GoogleOAuthOptions> opt)
    {
        _clientId = opt.Value.ClientId;
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