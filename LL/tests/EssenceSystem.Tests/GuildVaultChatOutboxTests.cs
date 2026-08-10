using System.Net;
using System.Text.Json;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Outbox;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Outbox;
using Microsoft.Extensions.Options;
using Services.LL.Achievements;
using Services.LL.Outbox;

namespace EssenceSystem.Tests;

public sealed class GuildVaultChatOutboxTests
{
    [Fact]
    public void RegistryRoutesGuildVaultMessagesToChatConsumer()
    {
        var registry = new GameEventOutboxConsumerRegistry();

        Assert.Equal(
            [GameEventOutboxConsumerNames.GuildVaultChat],
            registry.GetConsumers(GameEventTypes.GuildVaultChatMessage));
    }

    [Fact]
    public async Task ConsumerSendsGuildMessageWithLinkedEquipmentSnapshot()
    {
        var handler = new RecordingHttpMessageHandler();
        var options = new AchievementSystemChatOptions
        {
            BaseUrl = "https://chat.example/",
            Secret = "test-secret",
            TimeoutSeconds = 2
        };
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var payload = new GuildVaultChatMessagePayload(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Vault Keeper",
            "withdrew",
            new EquipmentInstanceDto
            {
                Id = Guid.NewGuid(),
                DisplayName = "Heavy Helm",
                ItemBase = new()
                {
                    Id = "heavy-helm",
                    Name = "Heavy Helm",
                    ItemType = ItemType.Equipment
                },
                EquipmentBase = new EquipmentBase
                {
                    Id = "heavy-helm",
                    Name = "Heavy Helm",
                    ItemType = ItemType.Equipment,
                    EquipmentType = EquipmentType.Head
                },
                AttributeModifiers =
                [
                    new InstanceAttributeModifier(AttributeType.Power, 7)
                ]
            },
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        var message = new GameEventOutboxMessage
        {
            EventType = GameEventTypes.GuildVaultChatMessage,
            PayloadJson = JsonSerializer.Serialize(payload, jsonOptions)
        };
        var consumer = new GuildVaultChatGameEventOutboxConsumer(
            new HttpClient(handler),
            Options.Create(options),
            jsonOptions);

        await consumer.HandleAsync(message, CancellationToken.None);

        Assert.Equal("https://chat.example/api/v1/chat/GuildSystem", handler.RequestUri?.ToString());
        Assert.Equal("test-secret", handler.SystemSecret);
        using var request = JsonDocument.Parse(handler.Body!);
        Assert.Equal("Vault Keeper", request.RootElement.GetProperty("actorName").GetString());
        Assert.Equal("withdrew", request.RootElement.GetProperty("body").GetString());
        Assert.Equal(
            "Heavy Helm",
            request.RootElement.GetProperty("linkedItem").GetProperty("displayName").GetString());
        Assert.Equal(
            7,
            request.RootElement
                .GetProperty("linkedItem")
                .GetProperty("attributeModifiers")[0]
                .GetProperty("amount")
                .GetSingle());
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
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
