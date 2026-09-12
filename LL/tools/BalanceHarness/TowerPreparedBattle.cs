using System.Text.Json;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Resolution.Models;

namespace BalanceHarness;

/// <summary>
/// One privately owned, validated recipe on frozen content. Reuses production preparation and
/// the executor's compiled definitions; every simulation creates fresh actors, engine and RNG.
/// </summary>
internal sealed class TowerPreparedBattle
{
    public const string Mode = "prepared-v1";
    private readonly TowerBattleInput input;
    private readonly CombatEncounterRuntime template;
    private readonly CombatEngineExecutor executor;
    private readonly HashSet<int> seeds;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly JsonElement participants;
    public string ParticipantsHash { get; }

    internal TowerPreparedBattle(TowerBattleInput ownedInput, CombatEncounterRuntime ownedRuntime, CombatEngineExecutor ownedExecutor)
    {
        input = ownedInput; template = ownedRuntime; executor = ownedExecutor;
        if (template.HostileWaveFactory is not null || template.HostileReinforcementWaves.Count != 0)
            throw new InvalidDataException("Prepared Tower reuse supports the single guardian encounter only.");
        seeds = input.Scenario.Seeds.ToHashSet();
        using var timing = TowerPerformanceTrace.Measure("prepared.describe-once");
        participants = IdleBattleRunner.DescribeParticipants(template);
        ParticipantsHash = HarnessJson.Hash(participants);
    }

    public static void ValidateMode(string? mode)
    {
        if (mode is not null && mode != Mode) throw new InvalidDataException("Unknown Tower execution mode.");
    }

    public async Task<TowerBattleReport> RunAsync(int seed, bool detailed = false, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!seeds.Contains(seed)) throw new InvalidDataException("Prepared Tower seed is outside the frozen recipe schedule.");
        // The executor owns a mutable compiled-ability lookup. Never use the same worker concurrently.
        await gate.WaitAsync(token);
        try
        {
            token.ThrowIfCancellationRequested();
            TowerPerformanceTrace.BattleStarted();
            using var timing = TowerPerformanceTrace.Measure("battle.prepared");
            var trialInput = input with { Rules = input.Rules with { RandomSeed = seed, CaptureEventLog = detailed } };
            CombatEncounterRuntime runtime;
            using (TowerPerformanceTrace.Measure("prepared.clone-actors"))
            {
                var plan = template.Plan with { EncounterId = TowerBattleRunner.EncounterId(input.Scenario.Id, seed), RandomSeed = seed };
                // Existing production clone copies combat attributes, health/barrier and mutable collections.
                // Definition/gear/Essence snapshots remain privately owned and are only read by simulation.
                static CombatRuntimeParticipant Clone(CombatRuntimeParticipant p) => p with { Combatant = p.Combatant.DeepCloneForEncounter() };
                runtime = new(plan, template.FriendlyParticipants.Select(Clone).ToArray(), template.HostileParticipants.Select(Clone).ToArray());
            }
            Domain.Models.Combat.CombatResult result;
            using (TowerPerformanceTrace.Measure(detailed ? "engine.detailed" : "engine.simulation-without-checkpoints"))
                result = await executor.ExecuteSimulationAsync(runtime, trialInput.Rules, token);
            var report = TowerBattleRunner.CreateReport(runtime, trialInput, result, participants, detailed);
            // BattleSummary.From already removes catalog Definition references; all remaining mutable
            // result state belongs to this simulation. No extra serialization/copy of the report is needed.
            TowerPerformanceTrace.BattleCompleted();
            return report;
        }
        finally { gate.Release(); }
    }

    internal string TemplateDigest() => HarnessJson.Hash(IdleBattleRunner.DescribeParticipants(template));
}
