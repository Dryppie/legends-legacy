using Application.Interfaces.WebSockets;
using Application.MediatR.Behaviors;
using Application.UseCases.CharacterActions.Commands.ResolveCharacterAction;
using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.WebSockets.Contracts;
using Common.Primitives;
using Domain.Models.Essences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.LL;
using Services.LL.Essences;
using Services.LL.Synchronization;

namespace EssenceSystem.Tests;

public sealed class EssenceProgressStateSyncTests
{
    [Theory]
    [InlineData(1, 0, 30, true)]
    [InlineData(1, 132_830, 30, true)]
    [InlineData(10, 0, 30, false)]
    [InlineData(1, 0, 0, false)]
    public async Task Combat_invalidates_essences_only_when_progress_changes(
        int level, int currentXp, int award, bool shouldInvalidate)
    {
        await using var db = CreateDb();
        var essence = new PlayerEssence
        {
            Id = Guid.NewGuid(),
            CharacterId = Guid.NewGuid(),
            EssenceDefinitionId = "essence.test",
            Level = level,
            CurrentXp = currentXp
        };
        db.PlayerEssences.Add(essence);
        await db.SaveChangesAsync();

        var realtime = new RecordingRealtimeBroadcaster();
        var sync = new StateSyncService(db, realtime, TimeProvider.System);
        var behavior = CreateBehavior(db, sync);
        await behavior.Handle(new ResolveCharacterActionCommand(essence.CharacterId), _ =>
        {
            new EssenceProgressionService().GrantXp(
                essence, EssenceDefinitionValidatorTests.ValidDefinition(), award);
            // Timestamp-only changes must not invalidate capped or unchanged XP.
            essence.UpdatedAt = essence.UpdatedAt.AddSeconds(1);
            return Task.FromResult(Response<CharacterActionDto?>.Success(null));
        }, CancellationToken.None);

        var invalidations = realtime.Messages.OfType<StateInvalidated>()
            .Where(message => message.Scope == StateSyncScopes.Essences).ToArray();
        Assert.Equal(shouldInvalidate ? 1 : 0, invalidations.Length);
        var checkpoint = await sync.GetCheckpointAsync(essence.CharacterId, CancellationToken.None);
        Assert.Equal(shouldInvalidate ? 1L : 0L, checkpoint.Revisions[StateSyncScopes.Essences]);
    }

    [Fact]
    public async Task Twelve_hours_of_batched_xp_persist_with_one_essence_invalidation()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var essences = Enumerable.Range(0, 2).Select(_ => new PlayerEssence
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            EssenceDefinitionId = "essence.test",
            Level = 1
        }).ToArray();
        db.PlayerEssences.AddRange(essences);
        await db.SaveChangesAsync();

        var realtime = new RecordingRealtimeBroadcaster();
        var sync = new StateSyncService(db, realtime, TimeProvider.System);
        await CreateBehavior(db, sync).Handle(new ResolveCharacterActionCommand(characterId), _ =>
        {
            var progression = new EssenceProgressionService();
            var definition = EssenceDefinitionValidatorTests.ValidDefinition();
            // 12 hours at a ten-second cadence, awarded across internal batches.
            for (var remaining = 4_320; remaining > 0; remaining -= 100)
            {
                foreach (var essence in essences)
                    progression.GrantXp(essence, definition, Math.Min(remaining, 100) * 30);
            }
            return Task.FromResult(Response<CharacterActionDto?>.Success(null));
        }, CancellationToken.None);

        db.ChangeTracker.Clear();
        Assert.All(await db.PlayerEssences.ToListAsync(), essence => Assert.Equal(129_600, essence.CurrentXp));
        var message = Assert.IsType<StateInvalidated>(Assert.Single(realtime.Messages));
        Assert.Equal(StateSyncScopes.Essences, message.Scope);
        Assert.Equal(characterId, message.CharacterId);
        Assert.Equal(1, message.Revision);
    }

    private static TransactionBehavior<ResolveCharacterActionCommand, Response<CharacterActionDto?>> CreateBehavior(
        LLDbContext db, StateSyncService sync) => new(db, sync,
        NullLogger<TransactionBehavior<ResolveCharacterActionCommand, Response<CharacterActionDto?>>>.Instance);

    private static LLDbContext CreateDb() => new(new DbContextOptionsBuilder<LLDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    private sealed class RecordingRealtimeBroadcaster : IGameRealtimeBroadcaster
    {
        public List<GameRealtimeEvent> Messages { get; } = [];

        public Task PublishAsync(Audience audience, GameRealtimeEvent message, string sender,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
