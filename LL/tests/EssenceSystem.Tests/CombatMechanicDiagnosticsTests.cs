using System.Collections;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BalanceHarness;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

/// <summary>Fabricated mechanic boundaries only: never runs an encounter, damage resolution, seeds or replays.</summary>
[Trait("Category", "BalanceHarness")]
public sealed class CombatMechanicDiagnosticsTests : IDisposable
{
    private readonly IDisposable _guard = new TowerPerformanceTrace(_ =>
        throw new InvalidOperationException("Mechanic fixture entered a battle.")).Activate();
    public void Dispose() => _guard.Dispose();

    private static CombatMechanicDiagnostics Observer(int maximum = 8192) => new(["seal"], ["resonance"], ["pillars"], maximum);
    private static RuntimeCombatant Actor(string id = "owner", RuntimeCombatant? owner = null, string group = "group-1") =>
        new(id, id, CombatTeam.Hostile, new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 1000 }, [],
            isSummoned: owner is not null, summonOwner: owner, canBasicAttack: false,
            summonGroupId: owner is null ? null : "pillars", summonGroupInstanceId: owner is null ? null : group);
    private static CompiledStatus Resonance() => new()
    {
        Id = "resonance", Name = "Resonance", MaxStacks = 5, LockAtMaxStacks = true,
        StackingPolicy = AbilityStatusStackingPolicy.Stack, Tags = new HashSet<string>(),
        TriggersByEvent = new Dictionary<AbilityTriggerEvent, IReadOnlyList<CompiledTrigger>>()
    };
    private static FastCombatEngine Engine(CombatMechanicDiagnostics? observer = null) =>
        new(new Dictionary<string, CompiledStatus> { ["resonance"] = Resonance() },
            new FastCombatEngineOptions(CaptureEventLog: false)) { MechanicDiagnostics = observer };
    private static void Invoke(object target, string name, params object?[] args)
    {
        var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.NotNull(method);
        method.Invoke(target, args);
    }
    private static void Tick(FastCombatEngine engine, int tick) =>
        typeof(FastCombatEngine).GetField("_currentTick", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(engine, tick);
    private static void Grant(FastCombatEngine engine, RuntimeCombatant owner, string activation, int duration = 1, string id = "seal") =>
        Invoke(engine, "GrantBarrier", owner, owner, 100,
            new CompiledEffect { Id = id, StatsSource = id, DurationTicks = duration, LinkedEffectId = "pulse",
                AbilityTags = new HashSet<string>(), Tags = new HashSet<string>(), Conditions = [] },
            null, false, new[] { owner }, activation);
    private static RuntimeEffect Pulse(RuntimeCombatant source, RuntimeCombatant target, string activation) => new(
        new CompiledEffect { Id = "pulse", StatsSource = "pulse", Operation = AbilityEffectOperation.ModifyStatusStacks,
            StatusId = "absent-no-op", DurationTicks = 50, IntervalTicks = 1, ChancePercent = 100,
            AbilityTags = new HashSet<string>(), Tags = new HashSet<string>(), Conditions = [] },
        source, target, activationId: activation);

    [Fact]
    public void DefaultsRemainOffAndExistingOptionsHaveNoDiagnosticField()
    {
        Assert.Null(Engine().MechanicDiagnostics);
        Assert.DoesNotContain("diagnostic", JsonSerializer.Serialize(new FastCombatEngineOptions(), HarnessJson.Options), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new[] { "Battle", "Succeeded", "GuardianHealthRemainingPercent", "DisplayDurationSeconds" },
            typeof(TowerBattleReport).GetProperties().Select(p => p.Name));
    }

    [Fact]
    public void FiltersAndSnapshotsAreOwnedAndBounded()
    {
        var ids = new[] { "SEAL" };
        var observer = new CombatMechanicDiagnostics(ids, [], [], 1);
        ids[0] = "wrong";
        var engine = Engine(observer);
        var owner = Actor();
        Grant(engine, owner, "a", id: "ignored");
        Assert.Empty(observer.Snapshot().Events);
        Grant(engine, owner, "b");
        var first = observer.Snapshot();
        Grant(engine, owner, "c");
        var current = observer.Snapshot();
        Assert.Single(first.Events);
        Assert.False(first.Truncated);
        Assert.True(current.Truncated);
        Assert.Equal(1, current.DroppedObservations);
        Assert.Single(current.Events);
        Assert.Equal("SEAL", Assert.Single(current.BarrierEffectIds));
        Assert.Throws<NotSupportedException>(() => ((IList<CombatMechanicEvent>)current.Events).Clear());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65537)]
    public void InvalidBoundsFailBeforeAnEngineExists(int maximum) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Observer(maximum));

    [Fact]
    public void CollectorCannotBeSharedBetweenEnginesOrEncounters()
    {
        var observer = Observer();
        _ = Engine(observer);
        Assert.Throws<InvalidOperationException>(() => Engine(observer));
        Invoke(observer, "Begin");
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => Invoke(observer, "Begin")).InnerException);
        Assert.False(observer.Snapshot().Completed);
        Invoke(observer, "Complete", 42);
        Assert.True(observer.Snapshot().Completed);
        Assert.Equal(42, observer.Snapshot().FinalTick);
    }

    [Fact]
    public void BarrierExpiryRemovesOnlyMatchingActivationAndRecordsTargetAttempts()
    {
        var observer = Observer();
        var engine = Engine(observer);
        var owner = Actor();
        var target = Actor("target");
        Grant(engine, owner, "first");
        Grant(engine, owner, "second", duration: 20);
        owner.ActiveEffects.Add(Pulse(owner, owner, "first"));
        target.ActiveEffects.Add(Pulse(owner, target, "first"));
        target.ActiveEffects.Add(Pulse(owner, target, "second"));
        target.ActiveEffects.Add(Pulse(owner, target, "unwatched"));
        Tick(engine, 2);
        Invoke(engine, "TickEffects", new object[] { new[] { owner, target } });
        var attempts = observer.Snapshot().Events.Where(e => e.Kind == CombatMechanicEventKind.LinkedPeriodicApplication).ToArray();
        Assert.Equal(3, attempts.Length);
        Assert.Equal(2, attempts.Count(e => e.ActivationId == "first"));
        Assert.Equal(new[] { "owner", "target" }, attempts.Where(e => e.ActivationId == "first").Select(e => e.EntityId));
        Invoke(engine, "TickBarrierContributions", new object[] { new[] { owner, target } });
        Assert.Empty(owner.ActiveEffects);
        Assert.Equal(new[] { "second", "unwatched" }, target.ActiveEffects.Select(e => e.ActivationId));
        var ended = Assert.Single(observer.Snapshot().Events, e => e.Kind == CombatMechanicEventKind.BarrierTimedOut);
        Assert.Equal("first", ended.ActivationId);
        Assert.Equal(2, ended.Tick);
        Assert.Equal(observer.Snapshot().Events[0].ApplicationOrder, ended.ApplicationOrder);
        Tick(engine, 3);
        Invoke(engine, "TickEffects", new object[] { new[] { owner, target } });
        Assert.Equal(2, observer.Snapshot().Events.Count(e => e.Kind == CombatMechanicEventKind.LinkedPeriodicApplication && e.ActivationId == "first"));
    }

    [Fact]
    public void SyntheticBreakIsDistinctFromTimeoutEvenWithLockedStatus()
    {
        var observer = Observer();
        var owner = Actor();
        owner.Statuses.Add(new RuntimeStatus(Resonance(), owner, owner, 5));
        Invoke(observer, "Barrier", 5, CombatMechanicEventKind.BarrierBroken,
            owner, owner, "seal", "cast-1", "pulse", 7L, 10d);
        var observation = Assert.Single(observer.Snapshot().Events);
        Assert.Equal(CombatMechanicEventKind.BarrierBroken, observation.Kind);
        Assert.Equal("cast-1", observation.ActivationId);
        Assert.True(owner.Statuses[0].IsRemovalLocked);
        // The linked-effect removal primitive must not consult Resonance or another activation.
        owner.ActiveEffects.Add(Pulse(owner, owner, "cast-1"));
        owner.ActiveEffects.Add(Pulse(owner, owner, "cast-2"));
        typeof(FastCombatEngine).GetMethod("RemoveLinkedActiveEffects", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, ["cast-1", "pulse", new[] { owner }]);
        Assert.Equal("cast-2", Assert.Single(owner.ActiveEffects).ActivationId);
    }

    [Fact]
    public void StackObservationsUseActualClampedStateAndFirstLockTick()
    {
        var observer = Observer();
        var engine = Engine(observer);
        var owner = Actor();
        Tick(engine, 10);
        Invoke(engine, "ApplyStatus", owner, owner, "resonance", 4, new[] { owner }, null, false, 1d);
        Tick(engine, 12);
        Invoke(engine, "ModifyStatusStacks", owner, owner, "resonance", 3, new[] { owner });
        Tick(engine, 14);
        Invoke(engine, "ModifyStatusStacks", owner, owner, "resonance", -2, new[] { owner });
        var events = observer.Snapshot().Events;
        Assert.Equal(new double[] { 4, 5, 5 }, events.Select(e => e.Value));
        Assert.Equal(12, events.First(e => e.Locked == true).Tick);
        Assert.Equal("unchanged", events[2].Reason);
        Assert.Equal(5, Assert.Single(owner.Statuses).Stacks);
    }

    [Fact]
    public void RemovingUnlockedStacksRecordsZeroAndKeepsSnapshotsStable()
    {
        var observer = Observer();
        var engine = Engine(observer);
        var owner = Actor();
        Invoke(engine, "ApplyStatus", owner, owner, "resonance", 2, new[] { owner }, null, false, 1d);
        var before = observer.Snapshot();
        Invoke(engine, "ModifyStatusStacks", owner, owner, "resonance", -2, new[] { owner });
        Assert.Empty(owner.Statuses);
        Assert.Equal(2, Assert.Single(before.Events).Value);
        Assert.Equal("removed", observer.Snapshot().Events.Last().Reason);
        Assert.Equal(0, observer.Snapshot().Events.Last().Value);
    }

    private static void Group(FastCombatEngine engine, RuntimeCombatant owner, params RuntimeCombatant[] members)
    {
        var type = typeof(FastCombatEngine).GetNestedType("RuntimeSummonGroup", BindingFlags.NonPublic)!;
        var group = Activator.CreateInstance(type, "group-1", "pillars", owner, 9)!;
        var list = (IList)type.GetProperty("Members")!.GetValue(group)!;
        foreach (var member in members) list.Add(member);
        var groups = (IDictionary)typeof(FastCombatEngine).GetField("_summonGroups", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(engine)!;
        groups.Add("group-1", group);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TimedGroupResolutionCountsOnlySurvivors(int survivors)
    {
        var observer = Observer();
        var engine = Engine(observer);
        var owner = Actor();
        var members = new[] { Actor("pillar-1", owner), Actor("pillar-2", owner) };
        foreach (var member in members.Skip(survivors))
            member.SetHealth(0); // Fabricated prior kills; no damage resolver or encounter.
        Group(engine, owner, members);
        var actors = new[] { owner }.Concat(members).ToArray();
        Tick(engine, 8);
        Invoke(engine, "TickSummons", new object[] { actors });
        Assert.Empty(observer.Snapshot().Events);
        Tick(engine, 9);
        Invoke(engine, "TickSummons", new object[] { actors });
        var events = observer.Snapshot().Events;
        Assert.Equal(survivors, events.Count(e => e.Kind == CombatMechanicEventKind.SummonTimedOut));
        Assert.Equal(survivors, Assert.Single(events, e => e.Kind == CombatMechanicEventKind.SummonGroupResolved).Value);
        Assert.Equal(CombatMechanicEventKind.SummonGroupResolved, events.Last().Kind);
        Assert.All(events, e => Assert.Equal("group-1", e.GroupInstanceId));
    }

    [Fact]
    public void OwnerDeathCleanupNeverBecomesTimedResolution()
    {
        var observer = Observer();
        var engine = Engine(observer);
        var owner = Actor();
        var pillar = Actor("pillar", owner);
        Group(engine, owner, pillar);
        owner.SetHealth(0);
        Tick(engine, 3);
        Invoke(engine, "ExpireOwnedSummons", owner, new[] { owner, pillar }, "owner death");
        Tick(engine, 9);
        Invoke(engine, "TickSummons", new object[] { new[] { owner, pillar } });
        var ended = Assert.Single(observer.Snapshot().Events);
        Assert.Equal(CombatMechanicEventKind.SummonOwnerDied, ended.Kind);
        Assert.Equal(3, ended.Tick);
        Assert.False(pillar.IsAlive);
    }

    [Fact]
    public void DeathLoggingRecordsKillWithCompactLoggingDisabled()
    {
        var observer = Observer();
        var engine = Engine(observer);
        var owner = Actor();
        var pillar = Actor("pillar", owner);
        var method = typeof(FastCombatEngine).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => m.Name == "Log" && m.GetParameters()[5].ParameterType == typeof(string));
        var args = method.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : null).ToArray();
        args[0] = owner; args[1] = pillar; args[2] = "synthetic"; args[3] = EventType.Death;
        args[4] = 0; args[5] = "fabricated death boundary";
        method.Invoke(engine, args);
        Assert.Equal(CombatMechanicEventKind.SummonKilled, Assert.Single(observer.Snapshot().Events).Kind);
    }

    private static string ArchiveFixture([CallerFilePath] string file = "") =>
        Path.Combine(Path.GetDirectoryName(file)!, "Fixtures", "mechanic-compact-report-v1.json.gz");

    [Fact]
    public void SeparateTraceRoundTripsSpawnIdentityFiltersAndTruncation()
    {
        var observer = Observer(1);
        var owner = Actor();
        var pillar = Actor("pillar", owner, "cast-7");
        Invoke(observer, "Summon", 4, pillar, CombatMechanicEventKind.SummonSpawned, null, 123);
        Invoke(observer, "Summon", 5, pillar, CombatMechanicEventKind.SummonKilled, null, null);
        Invoke(observer, "Complete", 9);
        var trace = observer.Snapshot();
        var restored = JsonSerializer.Deserialize<CombatMechanicTrace>(JsonSerializer.Serialize(trace, HarnessJson.Options), HarnessJson.Options)!;
        Assert.Equal(HarnessJson.Hash(trace), HarnessJson.Hash(restored));
        Assert.True(restored.Completed);
        Assert.True(restored.Truncated);
        Assert.Equal(1, restored.SchemaVersion);
        var spawn = Assert.Single(restored.Events);
        Assert.Equal("cast-7", spawn.GroupInstanceId);
        Assert.Equal("owner", spawn.SourceId);
        Assert.Equal("pillar", spawn.EntityId);
        Assert.Equal("pillars", spawn.DefinitionId);
        Assert.Equal(123, spawn.ScheduledEndTick);
    }

    [Fact]
    public void ExistingCompactArchiveRoundTripsWithoutAddingDiagnostics()
    {
        using var file = File.OpenRead(ArchiveFixture());
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var json = JsonDocument.Parse(gzip);
        var report = json.RootElement.Deserialize<TowerBattleReport>(HarnessJson.Options)!;
        Assert.Equal(HarnessJson.Hash(json.RootElement), HarnessJson.Hash(report));
        Assert.DoesNotContain("diagnostics", JsonSerializer.Serialize(report, HarnessJson.Options), StringComparison.OrdinalIgnoreCase);
    }
}
