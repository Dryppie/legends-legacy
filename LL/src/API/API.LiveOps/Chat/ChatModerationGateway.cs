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

    public async Task<ChatModerationStateGatewayResult> GetStateAsync(
        Guid characterId,
        int historyLimit,
        CancellationToken cancellationToken)
    {
        if (!TryCreateConfiguredRequest(
                HttpMethod.Get,
                $"api/v1/chat/Moderation/{characterId}?take={Math.Clamp(historyLimit, 1, 100)}",
                out var request,
                out var configurationError))
        {
            return new ChatModerationStateGatewayResult(
                false,
                null,
                [],
                configurationError);
        }

        using (request)
        using (var timeoutCts = CreateTimeoutToken(cancellationToken))
        {
            try
            {
                using var response = await httpClient.SendAsync(
                    request,
                    timeoutCts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    return new ChatModerationStateGatewayResult(
                        false,
                        null,
                        [],
                        $"Chat moderation state returned status {(int)response.StatusCode}.");
                }

                var body = await response.Content.ReadFromJsonAsync<ChatModerationStateResponse>(
                    cancellationToken: timeoutCts.Token);
                return body is null
                    ? new ChatModerationStateGatewayResult(
                        false,
                        null,
                        [],
                        "Chat moderation returned an invalid state response.")
                    : new ChatModerationStateGatewayResult(
                        true,
                        body.ActiveMute is null
                            ? null
                            : new ChatRestrictionGatewaySnapshot(
                                body.ActiveMute.Id,
                                body.ActiveMute.CharacterId,
                                body.ActiveMute.Reason,
                                body.ActiveMute.CreatedBySubject,
                                body.ActiveMute.CreatedAt,
                                body.ActiveMute.ExpiresAt,
                                body.ActiveMute.RevokedBySubject,
                                body.ActiveMute.RevokedAt,
                                body.ActiveMute.RevocationReason),
                        body.History.Select(x => new ChatModerationHistoryGatewayEntry(
                                x.OperationId,
                                x.ActionType,
                                x.CharacterId,
                                x.RestrictionId,
                                x.ActorSubject,
                                x.ActorDisplayName,
                                x.Reason,
                                x.OccurredAt))
                            .ToList(),
                        string.Empty);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new ChatModerationStateGatewayResult(
                    false,
                    null,
                    [],
                    "Chat moderation state timed out.");
            }
            catch (HttpRequestException)
            {
                return new ChatModerationStateGatewayResult(
                    false,
                    null,
                    [],
                    "Chat moderation state is temporarily unavailable.");
            }
        }
    }

    public async Task<ChatModerationAuditGatewayResult> GetAuditAsync(
        ChatModerationAuditGatewayQuery query,
        CancellationToken cancellationToken)
    {
        var parameters = new List<string>();
        AddParameter(parameters, "from", query.From?.ToString("O"));
        AddParameter(parameters, "to", query.To?.ToString("O"));
        AddParameter(parameters, "actionType", query.ActionType);
        AddParameter(parameters, "actor", query.Actor);
        AddParameter(parameters, "reference", query.Reference);
        AddParameter(parameters, "operationId", query.OperationId?.ToString());
        foreach (var characterId in query.CharacterIds)
        {
            AddParameter(parameters, "characterId", characterId.ToString());
        }
        AddParameter(parameters, "restrictionId", query.RestrictionId?.ToString());
        AddParameter(parameters, "beforeOccurredAt", query.BeforeOccurredAt?.ToString("O"));
        AddParameter(parameters, "beforeOperationId", query.BeforeOperationId?.ToString());
        AddParameter(parameters, "take", Math.Clamp(query.Limit, 1, 101).ToString());

        var relativePath = "api/v1/chat/ModerationAudit?" + string.Join('&', parameters);
        if (!TryCreateConfiguredRequest(
                HttpMethod.Get,
                relativePath,
                out var request,
                out var configurationError))
        {
            return new ChatModerationAuditGatewayResult(false, [], configurationError);
        }

        using (request)
        using (var timeoutCts = CreateTimeoutToken(cancellationToken))
        {
            try
            {
                using var response = await httpClient.SendAsync(request, timeoutCts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    return new ChatModerationAuditGatewayResult(
                        false,
                        [],
                        $"Chat moderation audit returned status {(int)response.StatusCode}.");
                }

                var body = await response.Content.ReadFromJsonAsync<
                    IReadOnlyList<ChatModerationHistoryResponse>>(
                    cancellationToken: timeoutCts.Token);
                return body is null
                    ? new ChatModerationAuditGatewayResult(
                        false,
                        [],
                        "Chat moderation returned an invalid audit response.")
                    : new ChatModerationAuditGatewayResult(
                        true,
                        body.Select(x => new ChatModerationHistoryGatewayEntry(
                                x.OperationId,
                                x.ActionType,
                                x.CharacterId,
                                x.RestrictionId,
                                x.ActorSubject,
                                x.ActorDisplayName,
                                x.Reason,
                                x.OccurredAt))
                            .ToList(),
                        string.Empty);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new ChatModerationAuditGatewayResult(
                    false,
                    [],
                    "Chat moderation audit timed out.");
            }
            catch (HttpRequestException)
            {
                return new ChatModerationAuditGatewayResult(
                    false,
                    [],
                    "Chat moderation audit is temporarily unavailable.");
            }
        }
    }

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
        if (!TryCreateConfiguredRequest(
                HttpMethod.Post,
                relativePath,
                out var request,
                out var configurationError))
        {
            return new ChatModerationGatewayResult(
                false,
                null,
                false,
                configurationError);
        }

        using (request)
        using (var timeoutCts = CreateTimeoutToken(cancellationToken))
        {
            request.Content = JsonContent.Create(payload);

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
    }

    private bool TryCreateConfiguredRequest(
        HttpMethod method,
        string relativePath,
        out HttpRequestMessage request,
        out string error)
    {
        request = null!;
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.Secret))
        {
            error = "Chat moderation is not configured for LiveOps.";
            return false;
        }

        request = new HttpRequestMessage(method, BuildUri(relativePath));
        request.Headers.TryAddWithoutValidation(
            ModerationSecretHeader,
            _options.Secret);
        error = string.Empty;
        return true;
    }

    private CancellationTokenSource CreateTimeoutToken(
        CancellationToken cancellationToken)
    {
        var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(
            Math.Clamp(_options.TimeoutSeconds, 1, 30)));
        return timeoutCts;
    }

    private Uri BuildUri(string relativePath)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/') + "/";
        return new Uri(new Uri(baseUrl), relativePath);
    }

    private static void AddParameter(
        ICollection<string> parameters,
        string name,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        parameters.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
    }

    private sealed record ChatModerationResponse(
        Guid RestrictionId,
        bool WasAlreadyProcessed);

    private sealed record ChatModerationStateResponse(
        ChatRestrictionResponse? ActiveMute,
        IReadOnlyList<ChatModerationHistoryResponse> History);

    private sealed record ChatRestrictionResponse(
        Guid Id,
        Guid CharacterId,
        string Reason,
        string CreatedBySubject,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ExpiresAt,
        string? RevokedBySubject,
        DateTimeOffset? RevokedAt,
        string? RevocationReason);

    private sealed record ChatModerationHistoryResponse(
        Guid OperationId,
        string ActionType,
        Guid CharacterId,
        Guid RestrictionId,
        string ActorSubject,
        string ActorDisplayName,
        string Reason,
        DateTimeOffset OccurredAt);
}
