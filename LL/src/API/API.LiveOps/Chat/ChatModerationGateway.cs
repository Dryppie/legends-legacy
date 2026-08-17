using System.Net.Http.Json;
using Application.Interfaces.Services.LL.Administration;
using Microsoft.Extensions.Options;

namespace API.LiveOps.Chat;

public sealed class ChatModerationGateway(
    HttpClient httpClient,
    IOptions<ChatModerationOptions> options)
    : IChatModerationGateway
{
    private const string ModerationSecretHeader = "X-LL-Chat-Moderation-Secret";
    private readonly ChatModerationOptions _options = options.Value;

    public Task<ChatModerationGatewayResult> MuteAsync(
        ChatMuteGatewayRequest request,
        CancellationToken cancellationToken) =>
        SendAsync(
            "api/v1/chat/Mute",
            request,
            cancellationToken);

    public Task<ChatModerationGatewayResult> UnmuteAsync(
        ChatUnmuteGatewayRequest request,
        CancellationToken cancellationToken) =>
        SendAsync(
            "api/v1/chat/Unmute",
            request,
            cancellationToken);

    private async Task<ChatModerationGatewayResult> SendAsync<TRequest>(
        string relativePath,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.Secret))
        {
            return new ChatModerationGatewayResult(
                false,
                null,
                false,
                "Chat moderation is not configured for LiveOps.");
        }

        var timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 1, 30));
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        timeoutCts.CancelAfter(timeout);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            BuildUri(relativePath))
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.TryAddWithoutValidation(
            ModerationSecretHeader,
            _options.Secret);

        try
        {
            using var response = await httpClient.SendAsync(
                request,
                timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return new ChatModerationGatewayResult(
                    false,
                    null,
                    false,
                    $"Chat moderation rejected the request with status {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<ChatModerationResponse>(
                cancellationToken: timeoutCts.Token);
            return body is null || body.RestrictionId == Guid.Empty
                ? new ChatModerationGatewayResult(
                    false,
                    null,
                    false,
                    "Chat moderation returned an invalid response.")
                : new ChatModerationGatewayResult(
                    true,
                    body.RestrictionId,
                    body.WasAlreadyProcessed,
                    string.Empty);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ChatModerationGatewayResult(
                false,
                null,
                false,
                "Chat moderation timed out.");
        }
        catch (HttpRequestException)
        {
            return new ChatModerationGatewayResult(
                false,
                null,
                false,
                "Chat moderation is temporarily unavailable.");
        }
    }

    private Uri BuildUri(string relativePath)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/') + "/";
        return new Uri(new Uri(baseUrl), relativePath);
    }

    private sealed record ChatModerationResponse(
        Guid RestrictionId,
        bool WasAlreadyProcessed);
}
