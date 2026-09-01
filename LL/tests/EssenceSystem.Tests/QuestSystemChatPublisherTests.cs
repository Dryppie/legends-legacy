using System.Net;
using System.Text.Json;
using Application.Interfaces.Services.LL.Quests;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Services.LL.Quests;

namespace EssenceSystem.Tests;

public sealed class QuestSystemChatPublisherTests
{
    [Fact]
    public async Task PublishAsync_persists_and_broadcasts_a_personal_system_message_with_a_stable_id()
    {
        var handler = new RecordingHttpMessageHandler();
        var publisher = new QuestSystemChatPublisher(
            new HttpClient(handler),
            Options.Create(new QuestSystemChatOptions
            {
                BaseUrl = "https://chat.example/",
                Secret = "test-secret"
            }),
            NullLogger<QuestSystemChatPublisher>.Instance);
        var characterId = Guid.NewGuid();
        var completion = new QuestCompletionChatMessage(
            "quest.onboarding.training_day",
            "Hunt the Hollow Stag");

        await publisher.PublishAsync(characterId, [completion], CancellationToken.None);
        await publisher.PublishAsync(characterId, [completion], CancellationToken.None);

        Assert.Equal(2, handler.RequestBodies.Count);
        var requests = handler.RequestBodies
            .Select(body => JsonDocument.Parse(body))
            .ToList();
        try
        {
            Assert.All(requests, request =>
            {
                var root = request.RootElement;
                Assert.Equal(
                    "Quest completed: Hunt the Hollow Stag.",
                    root.GetProperty("body").GetString());
                Assert.False(root.GetProperty("isGlobal").GetBoolean());
                Assert.Equal(
                    characterId,
                    root.GetProperty("targetCharacterId").GetGuid());
                Assert.Equal("System", root.GetProperty("senderName").GetString());
                Assert.True(root.GetProperty("broadcast").GetBoolean());
            });

            Assert.Single(requests
                .Select(request => request.RootElement.GetProperty("messageId").GetGuid())
                .Distinct());
        }
        finally
        {
            foreach (var request in requests)
            {
                request.Dispose();
            }
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal("test-secret", request.Headers.GetValues("X-LL-System-Chat-Secret").Single());
            RequestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
