using System.Net;
using System.Text.Json;
using Application.UseCases.Achievements.Dtos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Services.LL.Achievements;

namespace EssenceSystem.Tests;

public sealed class AchievementSystemChatPublisherTests
{
    [Fact]
    public async Task PublishAsync_uses_stable_message_ids_and_broadcasts_persisted_messages()
    {
        var handler = new RecordingHttpMessageHandler();
        var publisher = new AchievementSystemChatPublisher(
            new HttpClient(handler),
            Options.Create(new AchievementSystemChatOptions
            {
                BaseUrl = "https://chat.example/",
                Secret = "test-secret"
            }),
            NullLogger<AchievementSystemChatPublisher>.Instance);
        var characterId = Guid.NewGuid();
        var unlock = new AchievementUnlockDto
        {
            UnlockId = Guid.NewGuid(),
            AchievementKey = "dungeon.deathless_run",
            AchievementName = "Deathless Run",
            PlayerSystemMessage = "Achievement unlocked: Deathless Run (+50 points).",
            GlobalSystemMessage = "Hero unlocked Deathless Run (+50 points)."
        };

        await publisher.PublishAsync(characterId, [unlock], CancellationToken.None);
        await publisher.PublishAsync(characterId, [unlock], CancellationToken.None);

        Assert.Equal(4, handler.RequestBodies.Count);
        var requests = handler.RequestBodies
            .Select(body => JsonDocument.Parse(body))
            .ToList();
        try
        {
            Assert.All(requests, request =>
                Assert.True(request.RootElement.GetProperty("broadcast").GetBoolean()));

            var playerMessageIds = requests
                .Where(request => request.RootElement.GetProperty("targetCharacterId").ValueKind != JsonValueKind.Null)
                .Select(request => request.RootElement.GetProperty("messageId").GetGuid())
                .Distinct()
                .ToList();
            var globalMessageIds = requests
                .Where(request => request.RootElement.GetProperty("targetCharacterId").ValueKind == JsonValueKind.Null)
                .Select(request => request.RootElement.GetProperty("messageId").GetGuid())
                .Distinct()
                .ToList();

            Assert.Single(playerMessageIds);
            Assert.Single(globalMessageIds);
            Assert.NotEqual(playerMessageIds[0], globalMessageIds[0]);
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
            RequestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
