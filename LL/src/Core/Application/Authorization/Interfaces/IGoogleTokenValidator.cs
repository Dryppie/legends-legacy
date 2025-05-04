using Google.Apis.Auth;

namespace Application.Authorization.Interfaces;
public interface IGoogleTokenValidator
{
    Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, CancellationToken ct);
}