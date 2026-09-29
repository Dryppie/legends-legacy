using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Prophecies;
using Domain.Models.Bonuses;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Prophecies;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Rewards.Dungeon;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Dungeons;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Inventories;
using static BalanceHarness.TowerGrowthProgression;

namespace BalanceHarness;

public sealed record TowerJourneyDay(DateTimeOffset At, int Level, IReadOnlyList<TowerProphecyOffer> Daily,
    TowerProphecyOffer Weekly, string? Selected);
public sealed record TowerJourneyEvent(DateTimeOffset At, string Kind, int Amount, int? EnemyCount = null, string? Creature = null);
public sealed record TowerJourneyRoom(int Room, DateTimeOffset At, int Experience, int Cinders, int Soulstones,
    int Enemies, int Kills, bool Victory, int DurationTicks);
public sealed record TowerJourneyDungeonReceipt(DateTimeOffset Started, DateTimeOffset Ended, int MasteryAtEntry,
    IReadOnlyList<TowerJourneyRoom> Rooms, int PendingExperience, int LostExperience, int AppliedExperience,
    int WithheldExperience, int EssenceExperience, Application.Interfaces.Services.LL.Dungeons.DungeonMasteryAwardResult Mastery);

/// <summary>Declared serial activity clock and native personal growth/source state. No player attendance is inferred.</summary>
public sealed partial class TowerJourneyProgression
{
    public static readonly DateTimeOffset Epoch = new(2026,9,28,0,0,0,TimeSpan.Zero);
    public bool Enabled { get; }
    public TowerGrowthProgression Growth { get; }
    public TowerEntrySources Sources { get; }
    public TowerGrowthStudy.MasteryRepository MasteryRepository { get; } = new();
    public DungeonMasteryService Mastery { get; }
    public List<TowerJourneyDay> Days { get; } = [];
    public List<TowerJourneyEvent> DungeonEvents { get; } = [];
    public long IdleSeconds { get; private set; }
    public long CombatTicks { get; private set; }
    public DateTimeOffset Now => Epoch.AddSeconds(IdleSeconds).AddTicks(CombatTicks * TimeSpan.TicksPerSecond / FastCombatEngine.TicksPerSecond).AddTicks(WaitingTicks);
    public long WithheldIdleExperience { get; private set; }
    public long WithheldDungeonExperience { get; internal set; }
    private PropheciesOverview? overview;
    private DateTime? observedDay;

    public TowerJourneyProgression(string root, OfflineContent content, FixtureCharacter character, bool enabled)
    {
        Enabled = enabled; Growth = new(root,content,character); Mastery = new(MasteryRepository);
        Sources = enabled ? new(root,character.Id,Growth.Character,Growth.Leveling) : new(root,character.Id);
    }
    public async Task Observe(CancellationToken ct)
    {
        if (observedDay == Now.UtcDateTime.Date) return;
        overview = await Sources.Overview(Now,ct);
        var daily = overview.DailyProphecies.Select(TowerProphecyOffers.Snapshot).ToArray();
        var weekly = TowerProphecyOffers.Snapshot(overview.GreaterProphecy);
        var selected = TowerProphecyOffers.Choose(overview.DailyProphecies);
        if (selected is not null && !await Sources.AcceptGeneratedOffer(selected.Id,Now,ct)) throw new InvalidDataException("Journey offer rejected.");
        Days.Add(new(Now,Growth.Character.Level,daily,weekly,selected?.ProphecyDefinitionId));
        observedDay = Now.UtcDateTime.Date;
    }
    public async Task Claim(CancellationToken ct)
    {
        if (overview is null) throw new InvalidDataException("Observe offers before activity.");
        foreach (var p in overview.DailyProphecies.Append(overview.GreaterProphecy)
            .Where(p => p.Status == ProphecyStatus.Completed && p.PeriodStart <= Now && Now < p.PeriodEnd))
            if (!await Sources.Claim(p,Now,ct)) throw new InvalidDataException("Journey claim rejected.");
        await Sources.ClaimMilestones(Now,ct);
    }
    public async Task Idle(string area, bool victory, int cadence, CancellationToken ct)
    {
        if (cadence != 10) throw new InvalidDataException("Changed declared cadence.");
        await Observe(ct);
        if (victory)
        {
            var xp = Growth.Areas.CalculateEncounterExperience(area,1);
            if (Enabled) await Growth.AwardIdle(xp,ct); else WithheldIdleExperience += xp;
            await Sources.TrackKills(1,Now,ct); await Sources.TrackWins(1,Now,ct);
            if (Enabled && Growth.LastEssenceExperience > 0) await Sources.TrackEssenceExperience(Growth.LastEssenceExperience,Now,ct);
            await Claim(ct);
        }
        else
            await Sources.Track(new(Growth.Character.Id,Now,ProphecyProgressKind.EncounterLost,EnemyCount:1),ct);
        IdleSeconds += cadence;
    }
    public void AdvanceCombat(int ticks)
    {
        if (ticks is < 0 or > 6000) throw new InvalidDataException("Combat clock outside declared bound.");
        CombatTicks = checked(CombatTicks + ticks);
    }
    public async Task Event(ProphecyProgressKind kind, int amount, CancellationToken ct, int? enemies = null, string? creature = null)
    {
        if (amount <= 0) return;
        await Sources.Track(new(Growth.Character.Id,Now,kind,amount,CreatureDefinitionId:creature,EnemyCount:enemies),ct);
        DungeonEvents.Add(new(Now,kind.ToString(),amount,enemies,creature));
    }
    public async Task<int> Assemble(CancellationToken ct)
    {
        var count = Sources.State().Items.GetValueOrDefault("sigil_fragment") / Sources.FragmentCost;
        if (count > 0 && !await Sources.Assemble("goblin_mines",count,ct)) throw new InvalidDataException("Journey assembly rejected.");
        return count;
    }
    public object State() => new { at = Now, IdleSeconds, CombatTicks, growth = Growth.State(Sources.Claims.Sum(c => (long)c.Reward.CharacterExperience)),
        Growth.DungeonExperience, WithheldIdleExperience, WithheldDungeonExperience, source = Sources.State() };
}

/// <summary>Native pending XP and claim lifecycle for a single fixed entry snapshot; other excluded loot is never spent.</summary>
public sealed class TowerJourneyDungeon
{
    private readonly TowerJourneyProgression journey;
    private readonly DungeonCombatRewardCalculator calculator;
    private readonly DateTimeOffset started;
    private readonly List<TowerJourneyRoom> rooms = [];
    public TowerJourneyDungeonReceipt? Receipt { get; private set; }
    public TowerJourneyDungeon(string root, TowerJourneyProgression journey)
    {
        this.journey = journey; started = journey.Now;
        calculator = new(Boundary<IBonusService>((m,_) => m.Name == "GetAggregatedAsync"
                ? ValueTask.FromResult<IReadOnlyDictionary<BonusKind,double>>(new Dictionary<BonusKind,double>()) : throw new InvalidDataException(m.Name)),
            Boundary<ILootService>((m,_) => m.Name == "GenerateIdleCombatLootAsync"
                ? Task.FromResult(new List<InventoryItem>()) : throw new InvalidDataException(m.Name)),
            new JsonDungeonRewardBalanceProvider(new ConfigurationBuilder().Build(),root,HarnessJson.Options),
            Boundary<IEssenceResonanceService>((m,_) => m.Name == "RollEssenceDropsAsync"
                ? Task.FromResult<IReadOnlyList<InventoryItem>>([]) : throw new InvalidDataException(m.Name)));
    }
    public Task BeforeAction(CancellationToken ct) => journey.Observe(ct);
    public async Task Apply(DungeonRun run, DungeonCombatRewardFacts facts, IDungeonRunRepository repository, CancellationToken ct)
    {
        var reward = await calculator.CalculateAsync(facts,ct);
        await new DungeonPendingRewardWriter(repository).AddAsync(facts,reward,ct);
        var encounter = facts.Encounters.Single();
        journey.AdvanceCombat(encounter.CombatResult.Duration);
        var kills = encounter.HostileCreatures.Where((_,i) => encounter.IsVictory ||
            i < encounter.CombatResult.EnemyTeam.Count && encounter.CombatResult.EnemyTeam[i].Health <= 0).Count();
        await journey.Event(encounter.IsVictory ? ProphecyProgressKind.EncounterWon : ProphecyProgressKind.EncounterLost,1,ct,encounter.HostileCreatures.Count);
        foreach (var creature in encounter.HostileCreatures.Where((_,i) => encounter.IsVictory ||
            i < encounter.CombatResult.EnemyTeam.Count && encounter.CombatResult.EnemyTeam[i].Health <= 0))
            await journey.Event(ProphecyProgressKind.CreatureDefeated,1,ct,creature:creature.Id.ToString());
        rooms.Add(new(facts.CurrentRoomIndex,journey.Now,reward.TotalExperience,reward.TotalCinders,reward.TotalSoulstones,
            encounter.HostileCreatures.Count,kills,encounter.IsVictory,encounter.CombatResult.Duration));
        // Claims are explicit interactions after resolution. XP never changes the captured combat snapshot.
        await journey.Claim(ct);
    }
    public async Task AfterAction(DungeonActionOutcome outcome, CancellationToken ct)
    {
        if (outcome is DungeonActionOutcome.CombatVictory or DungeonActionOutcome.RestSiteResolved or DungeonActionOutcome.RunCompleted)
            await journey.Event(ProphecyProgressKind.DungeonRoomCleared,1,ct);
        if (outcome == DungeonActionOutcome.RunCompleted) await journey.Event(ProphecyProgressKind.DungeonCompleted,1,ct);
        await journey.Claim(ct);
    }
    public async Task Finish(DungeonRun run, CancellationToken ct)
    {
        if (Receipt is not null) throw new InvalidDataException("Run finalized twice.");
        var mastery = await journey.Mastery.AwardRunMasteryAsync(run,ct);
        var retry = await journey.Mastery.AwardRunMasteryAsync(run,ct);
        if (!retry.AlreadyAwarded || retry.TotalExperience != mastery.TotalExperience) throw new InvalidDataException("Duplicate mastery.");
        var pending = run.PendingExperience;
        var lost = run.State.FailureAnalysis?.LostPendingLoot.Experience ?? 0;
        var applied = 0; var withheld = 0; var essence = 0;
        if (run.Status == DungeonRunStatus.Completed)
        {
            journey.Growth.BeginDungeonClaim();
            IExperienceRewardWriter writer = journey.Enabled ? journey.Growth.ExperienceWriter : Boundary<IExperienceRewardWriter>((m,a) => {
                if (m.Name != "AddSplitExperienceAsync") throw new InvalidDataException(m.Name);
                withheld += (int)a[1]!; return Task.CompletedTask;
            });
            var claimer = new DungeonRunRewardClaimer(writer,
                Boundary<ICurrencyRewardWriter>((m,_) => m.Name == "AddAsync" ? Task.CompletedTask : throw new InvalidDataException(m.Name)),
                Boundary<IItemBaseRepository>((m,_) => m.Name == "GetItemBasesByIdsAsync"
                    ? Task.FromResult<IReadOnlyDictionary<string,ItemBase>>(new Dictionary<string,ItemBase>()) : throw new InvalidDataException(m.Name)),
                new InventoryItemFactory(),Boundary<IInventoryService>());
            await claimer.ClaimAsync(run,ct);
            applied = journey.Enabled ? pending : 0;
            journey.Growth.RecordDungeonClaim(applied); journey.WithheldDungeonExperience += withheld;
            essence = journey.Growth.LastEssenceExperience;
            run.Status = DungeonRunStatus.RewardsClaimed; run.RewardsClaimedAt = journey.Now;
            await claimer.ClaimAsync(run,ct); // native idempotence guard
            run.Status = DungeonRunStatus.Completed; // preserve the terminal combat receipt's established schema
            if (essence > 0) await journey.Event(ProphecyProgressKind.EssenceXpGained,essence,ct);
            await journey.Claim(ct);
        }
        if (pending + lost != rooms.Sum(r => r.Experience)) throw new InvalidDataException("Pending XP conservation failed.");
        Receipt = new(started,journey.Now,run.State.MasteryLevelAtStart,rooms,pending,lost,applied,withheld,essence,mastery);
    }
}
