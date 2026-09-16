using System.Reflection;
using System.Text.Json;
using BalanceHarness;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

/// <summary>Synthetic damage boundaries and accounting only; no encounters, seed allocation or replay.</summary>
[Trait("Category", "BalanceHarness")]
public sealed class CombatBarrierDamageDiagnosticsTests : IDisposable
{
    private readonly IDisposable _guard = new TowerPerformanceTrace(_ =>
        throw new InvalidOperationException("Attribution fixture entered a battle.")).Activate();
    public void Dispose() => _guard.Dispose();
    private static CombatMechanicDiagnostics Observer(int maximum = 8192) =>
        new(["seal"], [], [], maximum) { CaptureBarrierDamage = true };
    private static RuntimeCombatant Actor(string id) => new(id, id, CombatTeam.Hostile,
        new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 1000 }, [], canBasicAttack: false);
    private static FastCombatEngine Engine(CombatMechanicDiagnostics? observer) =>
        new(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(CaptureEventLog: true))
        { MechanicDiagnostics = observer };
    private static object? Call(object target, string method, params object?[] args) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
    private static CompiledEffect Effect(string id = "seal") => new()
    { Id = id, StatsSource = id, DurationTicks = 1, LinkedEffectId = "pulse", AbilityTags = new HashSet<string>(), Tags = new HashSet<string>(), Conditions = [] };
    private static void Grant(FastCombatEngine engine, RuntimeCombatant provider, RuntimeCombatant target,
        string activation = "a", int amount = 100, string id = "seal") =>
        Call(engine, "GrantBarrier", provider, target, amount, Effect(id), null, false, new[] { provider, target }, activation);
    private static int Hit(FastCombatEngine engine, RuntimeCombatant attacker, RuntimeCombatant target, int amount,
        string delivery = "Periodic", CompiledEffect? effect = null, params RuntimeCombatant[] others)
    {
        var method = typeof(FastCombatEngine).GetMethod("ApplyDamage", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var parameters = method.GetParameters();
        var args = parameters.Select(p => p.HasDefaultValue ? p.DefaultValue : null).ToArray();
        object?[] required = [attacker, target, amount, AttackType.None, DamageType.None, effect,
            new[] { attacker, target }.Concat(others).ToArray(), "synthetic"];
        Array.Copy(required, args, required.Length);
        args[10] = Enum.Parse(parameters[10].ParameterType, delivery);
        return (int)method.Invoke(engine, args)!;
    }
    private static CombatMechanicTrace Complete(CombatMechanicDiagnostics observer)
    { Call(observer, "Complete", 10); return observer.Snapshot(); }
    private static CombatMechanicEvent Start(double amount = 100) =>
        new(1, CombatMechanicEventKind.BarrierStarted, "target", "provider", "seal", amount, "a", "pulse", 1);
    private static CombatMechanicEvent Consumed(double amount, int tick = 2) =>
        new(tick, CombatMechanicEventKind.BarrierDamageConsumed, "target", "provider", "seal", amount, "a", "pulse", 1)
        { DamageSource = new("attacker", null, null, "Poison", "Periodic") };
    private static CombatMechanicTrace Trace(params CombatMechanicEvent[] events) =>
        new(2, true, 10, 8192, false, 0, ["seal"], [], [], events);

    [Fact]
    public void TwoHitsReconcileAndSeparateProviderFromAttacker()
    {
        var observer = Observer(); var engine = Engine(observer);
        var attacker = Actor("attacker"); var target = Actor("target"); var provider = Actor("provider");
        Grant(engine, provider, target);
        Hit(engine, attacker, target, 35);
        Hit(engine, attacker, target, 80);
        var account = Assert.Single(CombatBarrierDamageAccounting.Analyze(Complete(observer)));
        Assert.True(account.Reconciled); Assert.Equal(100, account.DamageConsumed);
        Assert.Equal(new double[] { 35, 65 }, account.Contributions.Select(c => c.Observation.Value));
        Assert.All(account.Contributions, c =>
        {
            Assert.Equal("provider", c.Observation.SourceId);
            Assert.Equal("target", c.Observation.EntityId);
            Assert.Equal("attacker", c.Observation.DamageSource!.AttackerId);
            Assert.Null(c.Observation.DamageSource.EffectId);
        });
        Assert.Equal(985, target.Health);
    }

    [Fact]
    public void OverlappingContributionsAreRecordedBeforeBreakCallbacksAndExcludeOverflow()
    {
        var observer = Observer(); var engine = Engine(observer); var target = Actor("target");
        Grant(engine, Actor("first-provider"), target, "a", 40);
        Grant(engine, Actor("second-provider"), target, "b", 60);
        Hit(engine, Actor("attacker"), target, 130);
        var trace = Complete(observer);
        Assert.Equal(CombatMechanicEventKind.BarrierDamageConsumed, trace.Events[2].Kind);
        Assert.Equal(CombatMechanicEventKind.BarrierDamageConsumed, trace.Events[3].Kind);
        Assert.Equal(new double[] { 40, 60 }, CombatBarrierDamageAccounting.Analyze(trace).Select(a => a.DamageConsumed));
        Assert.All(CombatBarrierDamageAccounting.Analyze(trace), a => Assert.True(a.Reconciled));
        Assert.Equal(970, target.Health);
    }

    [Fact]
    public void FractionalConsumptionIsNotRoundedAndUnknownProviderRemainsUnknown()
    {
        var observer = Observer(); var engine = Engine(observer); var target = Actor("target");
        target.GrantBarrier(null, 25.5f, 1, "seal", activationId: "a", linkedEffectId: "pulse");
        Call(observer, "Barrier", 0, CombatMechanicEventKind.BarrierStarted, target, target, "seal", "a", "pulse", 1L, 25.5d);
        Hit(engine, Actor("attacker"), target, 30);
        var account = Assert.Single(CombatBarrierDamageAccounting.Analyze(Complete(observer)));
        Assert.True(account.Reconciled); Assert.Equal(25.5, account.DamageConsumed);
        Assert.Equal(string.Empty, Assert.Single(account.Contributions).Observation.SourceId);
    }

    [Fact]
    public void GuardAndCoverAttributeOnlyActualRecipientConsumption()
    {
        var observer = Observer(); var engine = Engine(observer);
        var target = Actor("target"); var guardian = Actor("guardian"); var attacker = Actor("attacker");
        Grant(engine, target, target); Grant(engine, guardian, guardian, "b");
        target.Conditions.Add(new(StandardConditionType.Guard, target, target, 1, 100, 0, 10, "guard"));
        Call(engine, "GrantCover", guardian, target, 50, 100, "cover", "cover", false);
        Hit(engine, attacker, target, 100, "Direct", null, guardian);
        var hits = observer.Snapshot().Events.Where(e => e.Kind == CombatMechanicEventKind.BarrierDamageConsumed).ToArray();
        Assert.Equal(75, hits.Sum(e => e.Value));
        Assert.Equal(38, Assert.Single(hits, e => e.EntityId == "guardian").Value);
        Assert.Equal("Redirected", Assert.Single(hits, e => e.EntityId == "guardian").DamageSource!.Delivery);
        Assert.Equal(37, Assert.Single(hits, e => e.EntityId == "target").Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonDamageSpendingRemainsAnUnreconciledResidual(bool timeout)
    {
        var observer = Observer(); var engine = Engine(observer); var target = Actor("target");
        Grant(engine, target, target); target.ConsumeBarrier(40);
        Hit(engine, Actor("attacker"), target, timeout ? 20 : 60);
        if (timeout) Call(engine, "TickBarrierContributions", new object[] { new[] { target } });
        var account = Assert.Single(CombatBarrierDamageAccounting.Analyze(Complete(observer)));
        Assert.False(account.Reconciled); Assert.Equal(40, account.UnattributedAmount);
        Assert.Equal(timeout ? 20 : 60, account.DamageConsumed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameTickPulseOrderingAndMultipleTargetDeduplicationArePreserved(bool pulseFirst)
    {
        var pulse = new CombatMechanicEvent(2, CombatMechanicEventKind.LinkedPeriodicApplication,
            "victim1", "target", "pulse", ActivationId: "a");
        var events = new List<CombatMechanicEvent> { Start() };
        if (pulseFirst) { events.Add(pulse); events.Add(pulse with { EntityId = "victim2" }); }
        events.Add(Consumed(100));
        events.Add(Start() with { Tick = 2, Kind = CombatMechanicEventKind.BarrierBroken, Value = 100 });
        // The break-first scenario correctly has no linked application after removal.
        var account = Assert.Single(CombatBarrierDamageAccounting.Analyze(Trace(events.ToArray())));
        Assert.True(account.Reconciled);
        Assert.Equal(pulseFirst ? 1 : 0, Assert.Single(account.Contributions).WavesAlreadyEmitted);
    }

    [Fact]
    public void TimeoutAndCensoringDoNotInventConsumedDamage()
    {
        var ended = Start() with { Tick = 3, Kind = CombatMechanicEventKind.BarrierTimedOut, Value = 75 };
        var complete = Assert.Single(CombatBarrierDamageAccounting.Analyze(Trace(Start(), Consumed(25), ended)));
        Assert.True(complete.Reconciled); Assert.Equal(25, complete.DamageConsumed);
        var censored = Assert.Single(CombatBarrierDamageAccounting.Analyze(Trace(Start(), Consumed(25))));
        Assert.False(censored.Reconciled); Assert.Null(censored.UnattributedAmount);
    }

    [Fact]
    public void CapRejectsAttributionAndKeepsSnapshotsStable()
    {
        var observer = Observer(2); var engine = Engine(observer); var target = Actor("target");
        Grant(engine, target, target); var before = observer.Snapshot();
        Hit(engine, Actor("attacker"), target, 100);
        var after = Complete(observer);
        Assert.Single(before.Events); Assert.False(before.Truncated);
        Assert.Equal(2, after.Events.Count); Assert.True(after.Truncated);
        Assert.Throws<ArgumentException>(() => CombatBarrierDamageAccounting.Analyze(after));
    }

    [Fact]
    public void InvalidMissingOrOvercountedDataCannotReconcile()
    {
        Assert.Throws<ArgumentException>(() => CombatBarrierDamageAccounting.Analyze(Trace(Consumed(10))));
        Assert.Throws<ArgumentException>(() => CombatBarrierDamageAccounting.Analyze(Trace(Start()) with { SchemaVersion = 1 }));
        Assert.Throws<ArgumentException>(() => CombatBarrierDamageAccounting.Analyze(Trace(Start()) with { Completed = false }));
        Assert.Throws<ArgumentException>(() => CombatBarrierDamageAccounting.Analyze(Trace(Start(), Consumed(double.NaN))));
        Assert.Throws<ArgumentException>(() => CombatBarrierDamageAccounting.Analyze(Trace(Start(),
            Start() with { Tick = 2, Kind = CombatMechanicEventKind.BarrierBroken },
            new(2, CombatMechanicEventKind.LinkedPeriodicApplication, "victim", "target", "pulse", ActivationId: "a"))));
        var over = Assert.Single(CombatBarrierDamageAccounting.Analyze(Trace(Start(), Consumed(101),
            Start() with { Tick = 3, Kind = CombatMechanicEventKind.BarrierBroken })));
        Assert.False(over.Reconciled); Assert.Equal(-1, over.UnattributedAmount);
    }

    [Fact]
    public void FiltersExcludeOtherBarriersAndSchemaTwoRoundTripsIdentity()
    {
        var observer = Observer(); var engine = Engine(observer); var target = Actor("target");
        Grant(engine, target, target, "ignored", 25, "other");
        Grant(engine, target, target, "a", 50);
        Hit(engine, Actor("attacker"), target, 100, effect: Effect("poison-effect"));
        var trace = Complete(observer); Assert.Equal(2, trace.SchemaVersion);
        var roundtrip = JsonSerializer.Deserialize<CombatMechanicTrace>(JsonSerializer.Serialize(trace, HarnessJson.Options), HarnessJson.Options)!;
        Assert.Equal(HarnessJson.Hash(trace), HarnessJson.Hash(roundtrip));
        var account = Assert.Single(CombatBarrierDamageAccounting.Analyze(roundtrip));
        Assert.Equal(50, account.DamageConsumed);
        var hit = Assert.Single(account.Contributions).Observation;
        Assert.Equal("poison-effect", hit.DamageSource!.EffectId); Assert.Equal("Periodic", hit.DamageSource.Delivery);
    }

    [Fact]
    public void DefaultTraceHasNoNewFieldsAndDamageDoesNotAddSchemaOneEvents()
    {
        var observer = new CombatMechanicDiagnostics(["seal"], [], []);
        var engine = Engine(observer); var target = Actor("target"); Grant(engine, target, target);
        Hit(engine, Actor("attacker"), target, 30);
        var trace = observer.Snapshot(); Assert.Equal(1, trace.SchemaVersion); Assert.Single(trace.Events);
        Assert.DoesNotContain("DamageSource", JsonSerializer.Serialize(trace));
        Assert.DoesNotContain("damageSource", JsonSerializer.Serialize(trace, HarnessJson.Options));
    }

    [Fact]
    public void SyntheticLogsStateAndRandomStreamsMatchWithObserverDisabledOrEnabled()
    {
        string Run(int mode)
        {
            var observer = mode == 0 ? null : new CombatMechanicDiagnostics(["seal"], [], []) { CaptureBarrierDamage = mode == 2 };
            var engine = Engine(observer); var target = Actor("target"); var attacker = Actor("attacker");
            Grant(engine, target, target); Hit(engine, attacker, target, 35, "Direct"); Hit(engine, attacker, target, 90);
            var log = typeof(FastCombatEngine).GetField("_log", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(engine);
            var streams = new[] { "_random", "_magnitudeRandom", "_targetingRandom" }.Select(n =>
                ((Random)typeof(FastCombatEngine).GetField(n, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(engine)!).Next()).ToArray();
            return JsonSerializer.Serialize(new { target.Health, target.Barrier, Log = log, Streams = streams }, HarnessJson.Options);
        }
        Assert.Equal(Run(0), Run(1)); Assert.Equal(Run(0), Run(2));
    }
}
