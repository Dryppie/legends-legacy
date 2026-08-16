using System.Net.Http.Json;
using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;
using Domain.Models.Outbox;
using Microsoft.Extensions.Options;
using Services.LL.Achievements;

namespace Services.LL.Outbox;

public sealed class WorldTowerChatGameEventOutboxConsumer(
    HttpClient httpClient,
    IOptions<AchievementSystemChatOptions> options,
    JsonSerializerOptions jsonOptions) : IGameEventOutboxConsumer
{
    private const string SystemMessageSecretHeader = "X-LL-System-Chat-Secret";
    private readonly AchievementSystemChatOptions _options = options.Value;

    public string Consumer => GameEventOutboxConsumerNames.WorldTowerChat;

    public bool CanHandle(string eventType) =>
        string.Equals(
            eventType,
            GameEventTypes.WorldTowerChatAnnouncement,
            StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(
        GameEventOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<WorldTowerChatAnnouncementPayload>(
            message.PayloadJson,
            jsonOptions) ?? throw new InvalidOperationException(
                "World Tower chat announcement payload is invalid.");

        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || string.IsNullOrWhiteSpace(_options.Secret))
            throw new InvalidOperationException("System chat publishing is not configured.");

        var timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 1, 30));
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildSystemMessageUri())
        {
            Content = JsonContent.Create(new SystemChatMessageRequest(
                payload.Body,
                IsGlobal: true,
                TargetCharacterId: null,
                SenderName: "World",
                MessageId: payload.MessageId,
                SentAt: payload.SentAt,
                TargetUrl: payload.TargetUrl,
                Broadcast: true))
        };
        request.Headers.TryAddWithoutValidation(SystemMessageSecretHeader, _options.Secret);

        using var response = await httpClient.SendAsync(request, timeoutCts.Token);
        response.EnsureSuccessStatusCode();
    }

    private Uri BuildSystemMessageUri()
    {
        var baseUrl = _options.BaseUrl!.TrimEnd('/') + "/";
        return new Uri(new Uri(baseUrl), "api/v1/chat/System");
    }

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
