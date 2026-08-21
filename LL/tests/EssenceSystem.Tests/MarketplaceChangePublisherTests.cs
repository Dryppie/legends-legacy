using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.UseCases.MarketPlaces;
using Application.WebSockets.Contracts;

namespace EssenceSystem.Tests;

public sealed class MarketplaceChangePublisherTests
{
    [Fact]
    public async Task PublishesOneOrderedWorldChangeSetWithDistinctAffectedCharacters()
    {
        var stateSync = new RecordingStateSyncService();
        var events = new RecordingPublisher();
        var publisher = new MarketplaceChangePublisher(stateSync, events);
        var firstCharacterId = Guid.NewGuid();
        var secondCharacterId = Guid.NewGuid();

        var changes = await publisher.PublishAsync(
            [],
            [],
            [],
            [secondCharacterId, firstCharacterId, secondCharacterId],
            "market-test",
            CancellationToken.None);

        Assert.Equal(1, changes.Version);
        Assert.Equal(
            new[] { firstCharacterId, secondCharacterId }.Order(),
            changes.AffectedCharacterIds);
        var publication = Assert.Single(events.Publications);
        Assert.IsType<Audience.World>(publication.Audience);
        Assert.Same(changes, Assert.IsType<MarketplaceChanged>(publication.Message).Changes);
    }

    private sealed class RecordingPublisher : IGameRealtimeBroadcaster
    {
        public List<(Audience Audience, GameRealtimeEvent Message)> Publications { get; } = [];

        public Task PublishAsync(
            Audience audience,
            GameRealtimeEvent message,
            string sender,
            CancellationToken cancellationToken = default)
        {
            Publications.Add((audience, message));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingStateSyncService : IStateSyncService
    {
        private long _marketplaceRevision;

        public IReadOnlyDictionary<string, long> GetChangedRevisions(Guid? characterId) =>
            _marketplaceRevision == 0
                ? new Dictionary<string, long>()
                : new Dictionary<string, long>
                {
                    [StateSyncScopes.Marketplace] = _marketplaceRevision
                };

        public Task InvalidateCharacterAsync(Guid characterId, string reason, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task InvalidateCharacterScopeAsync(Guid characterId, string scope, string reason, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task InvalidateWorldScopeAsync(string scope, string reason, CancellationToken cancellationToken = default)
        {
            _marketplaceRevision += 1;
            return Task.CompletedTask;
        }

        public Task<StateSyncCheckpoint> GetCheckpointAsync(Guid characterId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new StateSyncCheckpoint(
                characterId,
                GetChangedRevisions(characterId),
                DateTimeOffset.UtcNow));
    }
}
