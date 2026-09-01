using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.Services.LL.Quests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services.LL.Quests;

public sealed class QuestSystemChatPublisher(
    HttpClient httpClient,
    IOptions<QuestSystemChatOptions> options,
    ILogger<QuestSystemChatPublisher> logger) : IQuestSystemChatPublisher
{
    private const string SystemMessageSecretHeader = "X-LL-System-Chat-Secret";
    private readonly QuestSystemChatOptions _options = options.Value;

    public async Task PublishAsync(
        Guid characterId,
        IReadOnlyCollection<QuestCompletionChatMessage> completions,
        CancellationToken cancellationToken)
    {
        if (completions.Count == 0 ||
            string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.Secret))
        {
            return;
        }

        foreach (var completion in completions)
        {
            await PersistAsync(
                new SystemChatMessageRequest(
                    $"Quest completed: {completion.Title}.",
                    IsGlobal: false,
                    TargetCharacterId: characterId,
                    SenderName: "System",
                    MessageId: CreateMessageId(characterId, completion.QuestId),
                    SentAt: DateTimeOffset.UtcNow,
                    Broadcast: true),
                cancellationToken);
        }
    }

    private async Task PersistAsync(
        SystemChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 1, 30));
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildSystemMessageUri())
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.TryAddWithoutValidation(
                SystemMessageSecretHeader,
                _options.Secret);

            using var response = await httpClient.SendAsync(httpRequest, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Failed to persist quest completion system chat message. Status code: {StatusCode}",
                    response.StatusCode);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Timed out while persisting quest completion system chat message.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist quest completion system chat message.");
        }
    }

    private Uri BuildSystemMessageUri()
    {
        var baseUrl = _options.BaseUrl!.TrimEnd('/') + "/";
        return new Uri(new Uri(baseUrl), "api/v1/chat/System");
    }

    private static Guid CreateMessageId(Guid characterId, string questId)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes($"quest:{characterId:N}:{questId.ToLowerInvariant()}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    private sealed record SystemChatMessageRequest(
        string Body,
        bool IsGlobal,
        Guid? TargetCharacterId,
        string? SenderName,
        Guid? MessageId,
        DateTimeOffset? SentAt,
        bool Broadcast);
}
