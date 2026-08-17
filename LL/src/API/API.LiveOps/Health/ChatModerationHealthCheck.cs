using API.LiveOps.Chat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace API.LiveOps.Health;

public sealed class ChatModerationHealthCheck(
    IHttpClientFactory httpClientFactory,
    IOptions<ChatModerationOptions> options) : IHealthCheck
{
    private readonly ChatModerationOptions _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(NormalizeBaseUrl(_options.BaseUrl), UriKind.Absolute, out var baseUri))
        {
            return HealthCheckResult.Degraded("Chat moderation is not configured.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(
            Math.Clamp(_options.TimeoutSeconds, 1, 30)));

        try
        {
            using var response = await httpClientFactory.CreateClient().GetAsync(
                new Uri(baseUri, "healthz/ready"),
                timeout.Token);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded(
                    $"Chat readiness returned status {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded("Chat readiness timed out.");
        }
        catch (HttpRequestException exception)
        {
            return HealthCheckResult.Degraded(
                "Chat moderation is unavailable.",
                exception);
        }
    }

    private static string NormalizeBaseUrl(string value) =>
        value.TrimEnd('/') + "/";
}
