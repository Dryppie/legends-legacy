using System.Net;
using System.Text.Json;
using Application.UseCases.Outbox;
using Domain.Models.Outbox;
using Microsoft.Extensions.Options;
using Services.LL.Achievements;
using Services.LL.Outbox;

namespace EssenceSystem.Tests;

public sealed class RaidChatOutboxTests
{
    [Fact]
    public async Task Consumer_sends_versioned_channel_snapshot_to_chat_service()
    {
        var handler = new RecordingHttpMessageHandler();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var memberIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var payload = new RaidChatChannelSnapshotPayload(
            Guid.NewGuid(),
            7,
            true,
            memberIds,
            DateTimeOffset.UtcNow);
        var message = new GameEventOutboxMessage
        {
            EventType = GameEventTypes.RaidChatChannelSnapshot,
            PayloadJson = JsonSerializer.Serialize(payload, jsonOptions)
        };
        var consumer = new RaidChatGameEventOutboxConsumer(
            new HttpClient(handler),
            Options.Create(new AchievementSystemChatOptions
            {
                BaseUrl = "https://chat.example/",
                Secret = "test-secret",
                TimeoutSeconds = 2
            }),
            jsonOptions);

        await consumer.HandleAsync(message, CancellationToken.None);

        Assert.Equal(
            "https://chat.example/api/v1/chat/RaidChannel",
            handler.RequestUri?.ToString());
        Assert.Equal("test-secret", handler.SystemSecret);
        using var request = JsonDocument.Parse(handler.Body!);
        Assert.Equal(payload.RaidRunId, request.RootElement.GetProperty("raidRunId").GetGuid());
        Assert.Equal(7, request.RootElement.GetProperty("revision").GetInt64());
        Assert.True(request.RootElement.GetProperty("isOpen").GetBoolean());
        Assert.Equal(2, request.RootElement.GetProperty("memberCharacterIds").GetArrayLength());
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
