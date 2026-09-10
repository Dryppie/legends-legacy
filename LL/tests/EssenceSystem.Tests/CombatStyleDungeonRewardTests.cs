using Domain.Models.CombatStyles;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Dungeons;
using Services.LL.Combat.Layers.Rewards.Dungeon;
using Services.LL.Combat.Layers.Rewards.Models;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class CombatStyleDungeonRewardTests
{
    [Fact]
    public async Task Pending_rewards_preserve_base_xp_and_original_style_in_json_across_reload()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var run = new DungeonRun { Id = Guid.NewGuid(), CharacterId = Guid.NewGuid() };
        await using (var db = new LLDbContext(options))
        {
            db.DungeonRuns.Add(run);
            await db.SaveChangesAsync();
            var writer = new DungeonPendingRewardWriter(new DungeonRunRepository(db));
            var facts = new DungeonCombatRewardFacts(run.Id, run.CharacterId, 0, 1, 1, RoomType.Combat, null,
                new Dictionary<ItemType, double>(), [run.CharacterId], []) { CapturedCombatStyleId = CombatStyleIds.Bastion };
            await writer.AddAsync(facts, new(run.CharacterId, 200, 0, 0, [], []) { EligibleBaseExperience = 100 }, default);
            await db.SaveChangesAsync();
        }
        await using var read = new LLDbContext(options);
        var stored = await read.DungeonRuns.SingleAsync();
        Assert.Equal(200, stored.PendingExperience);
        Assert.Equal(100, stored.State.PendingCombatStyleBaseExperience);
        Assert.Equal(CombatStyleIds.Bastion, stored.State.CapturedCombatStyleId);
        Assert.False(stored.State.CombatStyleExperienceClaimed);
    }

    [Theory]
    [InlineData(DungeonRunStatus.Completed, 300)]
    [InlineData(DungeonRunStatus.Retreated, 120)]
    public async Task Claim_uses_captured_style_and_retained_base_xp_once(DungeonRunStatus status, long expected)
    {
        var styles = new CombatStyleRewardTests.RecordingStyles();
        var run = new DungeonRun { Id = Guid.NewGuid(), CharacterId = Guid.NewGuid(), Status = status };
        run.State.CapturedCombatStyleId = CombatStyleIds.Bastion;
        run.State.PendingCombatStyleBaseExperience = 300;
        run.State.SecuredLoot = new() { CombatStyleBaseExperience = 120, Soulstones = 1 };
        var claimer = new DungeonRunRewardClaimer(null!, new EmptyCurrency(), new EmptyItems(), null!, null!, styles);

        await claimer.ClaimAsync(run, default);
        await claimer.ClaimAsync(run, default);

        var award = Assert.Single(styles.Awards);
        Assert.Equal(CombatStyleIds.Bastion, award.Style);
        Assert.Equal(expected, award.Xp);
        Assert.True(run.State.CombatStyleExperienceClaimed);
    }

    [Fact]
    public async Task Legacy_pending_rewards_never_create_retroactive_style_xp()
    {
        var run = JsonSerializer.Deserialize<DungeonRun>("{\"Status\":1,\"State\":{}}")!;
        var styles = new CombatStyleRewardTests.RecordingStyles();
        await new DungeonRunRewardClaimer(null!, null!, new EmptyItems(), null!, null!, styles).ClaimAsync(run, default);
        Assert.Empty(styles.Awards);
    }

    [Fact]
    public async Task Snapshot_round_trip_preserves_resolved_tuning_and_old_snapshots_mean_no_style()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var id = Guid.NewGuid();
        await using (var db = new LLDbContext(options))
        {
            db.CharacterSnapshots.Add(new CharacterSnapshot { Id = id, Name = "Style", CharacterId = Guid.NewGuid(),
                CombatStyle = new() { CombatStyleId = CombatStyleIds.Conduit, Kind = CombatStyleKind.Conduit,
                    ContentVersion = "frozen-test", Level = 8, CoreRank = 4, RefinementId = CombatStyleIds.DeepReservoir,
                    UpgradeIds = [CombatStyleIds.FullCircuit], ChanneledPlayerEssenceId = Guid.NewGuid(),
                    Tuning = new() { ChanneledBaseMultiplier = .6, ChanneledPerCharge = .25, ChargeCap = 4 } } });
            db.CharacterSnapshots.Add(new CharacterSnapshot { Id = Guid.NewGuid(), Name = "Legacy", CharacterId = Guid.NewGuid() });
            await db.SaveChangesAsync();
        }
        await using var read = new LLDbContext(options);
        var snapshot = await read.CharacterSnapshots.SingleAsync(x => x.Id == id);
        Assert.Equal(8, snapshot.CombatStyle!.Level);
        Assert.Equal("frozen-test", snapshot.CombatStyle.ContentVersion);
        Assert.Equal(4, snapshot.CombatStyle.Tuning.ChargeCap);
        Assert.Equal(.25, snapshot.CombatStyle.Tuning.ChanneledPerCharge);
        Assert.Equal(CombatStyleIds.FullCircuit, Assert.Single(snapshot.CombatStyle.UpgradeIds));
        Assert.Null((await read.CharacterSnapshots.SingleAsync(x => x.Id != id)).CombatStyle);
    }

    private sealed class EmptyItems : IItemBaseRepository
    {
        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(new Dictionary<string, ItemBase>());
        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task AddMissingItemBasesAsync(IReadOnlyCollection<ItemBase> items, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class EmptyCurrency : Services.LL.Interfaces.Combat.Reward.ICurrencyRewardWriter
    {
        public Task AddAsync(Guid characterId, int cinders, int soulstones, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
