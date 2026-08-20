using System.Net.Http.Json;
using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;
using Domain.Models.Outbox;
using Microsoft.Extensions.Options;
using Services.LL.Achievements;

namespace Services.LL.Outbox;

public sealed class RaidChatGameEventOutboxConsumer(
    HttpClient httpClient,
    IOptions<AchievementSystemChatOptions> options,
    JsonSerializerOptions jsonOptions) : IGameEventOutboxConsumer
{
    private const string SystemMessageSecretHeader = "X-LL-System-Chat-Secret";
    private readonly AchievementSystemChatOptions chatOptions = options.Value;

    public string Consumer => GameEventOutboxConsumerNames.RaidChat;

    public bool CanHandle(string eventType) =>
        string.Equals(eventType, GameEventTypes.RaidChatAnnouncement, StringComparison.OrdinalIgnoreCase)
        || string.Equals(eventType, GameEventTypes.RaidChatChannelSnapshot, StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken)
    {
        if (string.Equals(
                message.EventType,
                GameEventTypes.RaidChatChannelSnapshot,
                StringComparison.OrdinalIgnoreCase))
        {
            await HandleChannelSnapshotAsync(message, cancellationToken);
            return;
        }

        var payload = JsonSerializer.Deserialize<RaidChatAnnouncementPayload>(message.PayloadJson, jsonOptions)
            ?? throw new InvalidOperationException("Raid chat announcement payload is invalid.");
        if (string.IsNullOrWhiteSpace(chatOptions.BaseUrl) || string.IsNullOrWhiteSpace(chatOptions.Secret))
            throw new InvalidOperationException("System chat publishing is not configured.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(chatOptions.TimeoutSeconds, 1, 30)));
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildSystemMessageUri())
        {
            Content = JsonContent.Create(new SystemChatMessageRequest(
                payload.Body,
                IsGlobal: true,
                TargetCharacterId: null,
                SenderName: "World",
                payload.MessageId,
                payload.SentAt,
                payload.TargetUrl,
                Broadcast: true))
        };
        request.Headers.TryAddWithoutValidation(SystemMessageSecretHeader, chatOptions.Secret);
        using var response = await httpClient.SendAsync(request, timeout.Token);
        response.EnsureSuccessStatusCode();
    }

    private async Task HandleChannelSnapshotAsync(
        GameEventOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<RaidChatChannelSnapshotPayload>(
            message.PayloadJson,
            jsonOptions) ?? throw new InvalidOperationException(
            "Raid chat channel snapshot payload is invalid.");
        if (string.IsNullOrWhiteSpace(chatOptions.BaseUrl) || string.IsNullOrWhiteSpace(chatOptions.Secret))
            throw new InvalidOperationException("System chat publishing is not configured.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(chatOptions.TimeoutSeconds, 1, 30)));
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildRaidChannelUri())
        {
            Content = JsonContent.Create(payload, options: jsonOptions)
        };
        request.Headers.TryAddWithoutValidation(SystemMessageSecretHeader, chatOptions.Secret);
        using var response = await httpClient.SendAsync(request, timeout.Token);
        response.EnsureSuccessStatusCode();
    }

    private Uri BuildSystemMessageUri() =>
        new(new Uri(chatOptions.BaseUrl!.TrimEnd('/') + "/"), "api/v1/chat/System");

    private Uri BuildRaidChannelUri() =>
        new(new Uri(chatOptions.BaseUrl!.TrimEnd('/') + "/"), "api/v1/chat/RaidChannel");

    private sealed record SystemChatMessageRequest(
        string Body,
        bool IsGlobal,
        Guid? TargetCharacterId,
        string? SenderName,
        Guid? MessageId,
        DateTimeOffset? SentAt,
        string? TargetUrl,
        bool Broadcast);
}
