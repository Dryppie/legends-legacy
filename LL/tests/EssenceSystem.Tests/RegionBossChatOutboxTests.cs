using System.Net;
using System.Text.Json;
using Application.UseCases.Outbox;
using Domain.Models.Outbox;
using Microsoft.Extensions.Options;
using Services.LL.Achievements;
using Services.LL.Outbox;

namespace EssenceSystem.Tests;

public sealed class RegionBossChatOutboxTests
{
    [Fact]
    public async Task Consumer_sends_region_boss_announcement_to_the_world_channel()
    {
        var handler = new RecordingHttpMessageHandler();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var payload = new RegionBossChatAnnouncementPayload(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "The Region Boss battle against The Mad King has begun!",
            "/game/world/shenic",
            new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero));
        var message = new GameEventOutboxMessage
        {
            EventType = GameEventTypes.RegionBossChatAnnouncement,
            PayloadJson = JsonSerializer.Serialize(payload, jsonOptions)
        };
        var consumer = new RegionBossChatGameEventOutboxConsumer(
            new HttpClient(handler),
            Options.Create(new AchievementSystemChatOptions
            {
                BaseUrl = "https://chat.example/",
                Secret = "test-secret",
                TimeoutSeconds = 2
            }),
            jsonOptions);

        await consumer.HandleAsync(message, CancellationToken.None);

        Assert.Equal("https://chat.example/api/v1/chat/System", handler.RequestUri?.ToString());
        Assert.Equal("test-secret", handler.SystemSecret);
        using var request = JsonDocument.Parse(handler.Body!);
        Assert.Equal(payload.Body, request.RootElement.GetProperty("body").GetString());
        Assert.Equal("World", request.RootElement.GetProperty("senderName").GetString());
        Assert.True(request.RootElement.GetProperty("isGlobal").GetBoolean());
        Assert.True(request.RootElement.GetProperty("broadcast").GetBoolean());
        Assert.Equal(payload.MessageId, request.RootElement.GetProperty("messageId").GetGuid());
        Assert.Equal(payload.TargetUrl, request.RootElement.GetProperty("targetUrl").GetString());
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? SystemSecret { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            SystemSecret = request.Headers.GetValues("X-LL-System-Chat-Secret").Single();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }
}
