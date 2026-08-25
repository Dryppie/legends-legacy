using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.Services.LL.Achievements;
using Application.UseCases.Achievements.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services.LL.Achievements;

public sealed class AchievementSystemChatPublisher : IAchievementSystemChatPublisher
{
    private const string SystemMessageSecretHeader = "X-LL-System-Chat-Secret";
    private readonly HttpClient _httpClient;
    private readonly AchievementSystemChatOptions _options;
    private readonly ILogger<AchievementSystemChatPublisher> _logger;

    public AchievementSystemChatPublisher(
        HttpClient httpClient,
        IOptions<AchievementSystemChatOptions> options,
        ILogger<AchievementSystemChatPublisher> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(
        Guid? characterId,
        IReadOnlyCollection<AchievementUnlockDto> unlocks,
        CancellationToken cancellationToken)
    {
        if (unlocks.Count == 0 ||
            string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.Secret))
        {
            return;
        }

        foreach (var unlock in unlocks)
        {
            if (characterId.HasValue && !string.IsNullOrWhiteSpace(unlock.PlayerSystemMessage))
            {
                await PersistAsync(
                    new SystemChatMessageRequest(
                        unlock.PlayerSystemMessage,
                        IsGlobal: false,
                        TargetCharacterId: characterId,
                        SenderName: "System",
                        MessageId: CreateMessageId(unlock.UnlockId, "player"),
                        SentAt: DateTimeOffset.UtcNow,
                        Broadcast: true),
                    cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(unlock.GlobalSystemMessage))
            {
                await PersistAsync(
                    new SystemChatMessageRequest(
                        unlock.GlobalSystemMessage,
                        IsGlobal: true,
                        TargetCharacterId: null,
                        SenderName: "World",
                        MessageId: CreateMessageId(unlock.UnlockId, "global"),
                        SentAt: DateTimeOffset.UtcNow,
                        Broadcast: true),
                    cancellationToken);
            }
        }
    }

    private async Task PersistAsync(SystemChatMessageRequest request, CancellationToken cancellationToken)
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
            httpRequest.Headers.TryAddWithoutValidation(SystemMessageSecretHeader, _options.Secret);

            using var response = await _httpClient.SendAsync(httpRequest, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to persist achievement system chat message. Status code: {StatusCode}",
                    response.StatusCode);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Timed out while persisting achievement system chat message.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist achievement system chat message.");
        }
    }

    private Uri BuildSystemMessageUri()
    {
        var baseUrl = _options.BaseUrl!.TrimEnd('/') + "/";
        return new Uri(new Uri(baseUrl), "api/v1/chat/System");
    }

    private static Guid CreateMessageId(Guid unlockId, string audience)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes($"achievement:{unlockId:N}:{audience}"));
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
