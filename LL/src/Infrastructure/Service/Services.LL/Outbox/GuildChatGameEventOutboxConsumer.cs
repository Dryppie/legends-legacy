using System.Net.Http.Json;
using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;
using Domain.Models.Outbox;
using Microsoft.Extensions.Options;
using Services.LL.Achievements;

namespace Services.LL.Outbox;

public sealed class GuildChatGameEventOutboxConsumer(
    HttpClient httpClient,
    IOptions<AchievementSystemChatOptions> options,
    JsonSerializerOptions jsonOptions) : IGameEventOutboxConsumer
{
    private const string SystemMessageSecretHeader = "X-LL-System-Chat-Secret";
    private readonly AchievementSystemChatOptions _options = options.Value;

    public string Consumer => GameEventOutboxConsumerNames.GuildChat;

    public bool CanHandle(string eventType) =>
        string.Equals(
            eventType,
            GameEventTypes.GuildChatMessage,
            StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(
        GameEventOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<GuildChatMessagePayload>(
            message.PayloadJson,
            jsonOptions) ?? throw new InvalidOperationException(
            "Guild chat payload is invalid.");

        if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.Secret))
        {
            throw new InvalidOperationException(
                "System chat publishing is not configured.");
        }

        var timeout = TimeSpan.FromSeconds(
            Math.Clamp(_options.TimeoutSeconds, 1, 30));
        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildGuildMessageUri())
        {
            Content = JsonContent.Create(
                new GuildChatMessageRequest(
                    payload.GuildId,
                    payload.ActorCharacterId,
                    payload.ActorName,
                    payload.Body,
                    null,
                    payload.MessageId,
                    payload.SentAt),
                options: jsonOptions)
        };
        request.Headers.TryAddWithoutValidation(
            SystemMessageSecretHeader,
            _options.Secret);

        using var response = await httpClient.SendAsync(
            request,
            timeoutCts.Token);
        response.EnsureSuccessStatusCode();
    }

    private Uri BuildGuildMessageUri()
    {
        var baseUrl = _options.BaseUrl!.TrimEnd('/') + "/";
        return new Uri(new Uri(baseUrl), "api/v1/chat/GuildSystem");
    }

    private sealed record GuildChatMessageRequest(
        Guid GuildId,
        Guid ActorCharacterId,
        string ActorName,
        string Body,
        JsonElement? LinkedItem,
        Guid MessageId,
        DateTimeOffset SentAt);
}
