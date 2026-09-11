using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.UseCases.Outbox;
using Domain.Models.Achievements;
using Domain.Models.Outbox;
using Microsoft.Extensions.Options;
using Services.LL.Achievements;
using Services.LL.Outbox;
using Services.LL.WorldTower;

namespace EssenceSystem.Tests;

public sealed class WorldTowerTitleCatalogTests
{
    [Fact]
    public void EveryReleasedFloorHasItsUniqueCharacterTitle()
    {
        var data = Path.Combine(TestContentPaths.FindApiRoot(), "Data");
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
        var floors = new JsonWorldTowerDefinitionProvider(Path.Combine(data, "world-tower", "tower-floors.json"), json).GetFloors();
        var titles = JsonSerializer.Deserialize<TitleDefinition[]>(File.ReadAllText(Path.Combine(data, "titles", "world-tower.json")), json)!;
        string[] names = ["Gatekeeper", "Bloodwing Huntress", "Broodkeeper", "Mirrorbound", "First Warden",
            "Ashen Bellkeeper", "Endless Spring", "Poisoned Vessel", "Ninefold", "Mad King", "Name-Eater",
            "Shackled Storm", "Moondrowned", "Smith of the Fallen Star", "Second Warden"];
        Assert.Equal(15, titles.Length);
        Assert.Equal(titles.Length, titles.Select(t => t.Key).Distinct().Count());
        Assert.Equal(titles.Length, titles.Select(t => t.Name).Distinct().Count());
        foreach (var floor in floors)
        {
            var title = Assert.Single(titles, t => t.Key == floor.RewardTitleKey);
            Assert.Equal(names[floor.FloorNumber - 1], title.Name);
            Assert.Equal(TitleScope.Character, title.Scope);
            Assert.Equal(AchievementCategory.WorldTower, title.Category);
            Assert.True(title.IsActive);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TitleMessagesArePrivateAndExistingTowerAnnouncementsRemainGlobal(bool personal)
    {
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var recipient = personal ? Guid.NewGuid() : (Guid?)null;
        var payload = new WorldTowerChatAnnouncementPayload(Guid.NewGuid(), Guid.NewGuid(), "Title unlocked: Gatekeeper.",
            "/game/character/achievements", DateTimeOffset.UtcNow, recipient);
        var handler = new CaptureHandler();
        var consumer = new WorldTowerChatGameEventOutboxConsumer(new HttpClient(handler),
            Options.Create(new AchievementSystemChatOptions { BaseUrl = "https://chat.example", Secret = "test-secret" }), json);
        await consumer.HandleAsync(new GameEventOutboxMessage { PayloadJson = JsonSerializer.Serialize(payload, json) }, CancellationToken.None);
        using var request = JsonDocument.Parse(handler.Body!);
        Assert.Equal(!personal, request.RootElement.GetProperty("isGlobal").GetBoolean());
        Assert.Equal(personal ? "System" : "World", request.RootElement.GetProperty("senderName").GetString());
        Assert.Equal(recipient, request.RootElement.GetProperty("targetCharacterId").Deserialize<Guid?>());
        Assert.True(request.RootElement.GetProperty("broadcast").GetBoolean());
        Assert.Equal(payload.MessageId, request.RootElement.GetProperty("messageId").GetGuid());
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
