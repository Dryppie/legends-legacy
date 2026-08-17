using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Guilds;
using Application.UseCases.Outbox;
using Domain.Models.Entities.Characters;
using Services.LL.Guilds;

namespace EssenceSystem.Tests;

public sealed class GuildSystemChatPublisherTests
{
    public static TheoryData<GuildSystemChatEvent, string> MessageBodies => new()
    {
        { GuildSystemChatEvent.Joined, "joined the guild." },
        { GuildSystemChatEvent.Kicked, "was kicked from the guild." },
        { GuildSystemChatEvent.Left, "left the guild." },
        { GuildSystemChatEvent.PromotedToOfficer, "was promoted to Officer." },
        { GuildSystemChatEvent.DemotedToMember, "was demoted to Member." },
        { GuildSystemChatEvent.Invited, "was invited to the guild." }
    };

    [Theory]
    [MemberData(nameof(MessageBodies))]
    public async Task PublishAsyncEnqueuesPersistentGuildSystemMessage(
        GuildSystemChatEvent eventType,
        string expectedBody)
    {
        var guildId = Guid.NewGuid();
        var character = new Character
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Ember"
        };
        var outbox = new RecordingOutbox();
        var publisher = new GuildSystemChatPublisher(
            new StubCharacterService(character),
            outbox);

        await publisher.PublishAsync(
            guildId,
            character.Id,
            eventType,
            CancellationToken.None);

        var call = Assert.Single(outbox.Calls);
        var payload = Assert.IsType<GuildChatMessagePayload>(call.Payload);
        Assert.Equal(GameEventTypes.GuildChatMessage, call.EventType);
        Assert.Equal(guildId, payload.GuildId);
        Assert.Equal(character.Id, payload.ActorCharacterId);
        Assert.Equal(character.Name, payload.ActorName);
        Assert.Equal(expectedBody, payload.Body);
        Assert.NotEqual(Guid.Empty, payload.MessageId);
        Assert.Equal(character.Id, call.CharacterId);
        Assert.Equal(character.UserId, call.AccountId);
    }

    [Theory]
    [InlineData(GuildBuildingChatEvent.Constructed, "built Mission Board to level 1.")]
    [InlineData(GuildBuildingChatEvent.Upgraded, "upgraded Mission Board to level 3.")]
    public async Task PublishBuildingAsyncIncludesBuildingNameAndResultingLevel(
        GuildBuildingChatEvent eventType,
        string expectedBody)
    {
        var character = new Character
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Ember"
        };
        var outbox = new RecordingOutbox();
        var publisher = new GuildSystemChatPublisher(
            new StubCharacterService(character),
            outbox);

        await publisher.PublishBuildingAsync(
            Guid.NewGuid(),
            character.Id,
            "Mission Board",
            eventType == GuildBuildingChatEvent.Constructed ? 1 : 3,
            eventType,
            CancellationToken.None);

        var payload = Assert.IsType<GuildChatMessagePayload>(
            Assert.Single(outbox.Calls).Payload);
        Assert.Equal(expectedBody, payload.Body);
    }

    private sealed class RecordingOutbox : IGameEventOutbox
    {
        public List<OutboxCall> Calls { get; } = [];

        public Task EnqueueAsync<TPayload>(
            string eventType,
            TPayload payload,
            Guid? characterId,
            Guid? accountId,
            CancellationToken cancellationToken)
        {
            Calls.Add(new OutboxCall(eventType, payload!, characterId, accountId));
            return Task.CompletedTask;
        }
    }

    private sealed record OutboxCall(
        string EventType,
        object Payload,
        Guid? CharacterId,
        Guid? AccountId);

    private sealed class StubCharacterService(Character character) : ICharacterService
    {
        public Task<Character?> GetBaseCharacterByIdAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            Task.FromResult<Character?>(characterId == character.Id ? character : null);

        public Task<Character> CreateCharacterAsync(Guid userId, string username, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetMyCharacterAsync(Guid currentUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetCharacterByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetMyCharacterOverviewAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetCharacterOverviewByNameAsync(string characterName, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> UpdateCharacterNameAsync(Guid userId, string username, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsCharacterNameTakenAsync(string name, Guid? excludedCharacterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetCharacterWithSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guid?> GetCharacterIdByNameAsync(string name, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
