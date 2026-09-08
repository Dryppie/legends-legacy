using System.Text.Json;
using Common.Randomness;
using Domain.Models.Combat;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;

namespace BalanceHarness;

public sealed class IdleBattleRunner(OfflineContent content)
{
    public async Task<CombatEncounterRuntime> PrepareAsync(IdleBattleInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        content.Validate(input);
        var character = input.Character.Materialize(content.Equipment);
        var creatures = OfflineContent.ReadCreatures(input);
        var area = OfflineContent.ReadArea(input);
        var setup = content.CreateSetup(character, input.Character.MaterializeEssences());
        var pipeline = new CombatPreparationPipeline(setup);
        var plan = CreatePlan(input);
        var requests = new List<CombatantPreparationRequest>
        {
            new(plan.FriendlyParticipants.Single(), new LiveCombatantPreparationSource(character))
        };
        requests.AddRange(plan.HostileParticipants.Select((slot, index) =>
            new CombatantPreparationRequest(slot, new LiveCombatantPreparationSource(creatures[index], area))));
        var participants = await pipeline.PrepareAsync(CombatContentType.Idle, requests, cancellationToken);
        return new(plan, participants.Where(x => x.Slot.Side == CombatSide.Friendly).ToArray(),
            participants.Where(x => x.Slot.Side == CombatSide.Hostile).ToArray());
    }

    public async Task<BattleReport> RunAsync(IdleBattleInput input, bool detailed = false,
        CancellationToken cancellationToken = default)
    {
        var runtime = await PrepareAsync(input, cancellationToken);
        var prepared = DescribeParticipants(runtime);
        // Fresh executor per battle: its compiled-essence cache is mutable.
        var result = await content.CreateExecutor().ExecuteSimulationAsync(runtime,
            input.Rules with { CaptureEventLog = detailed }, cancellationToken);
        var resolved = new CombatEncounterResultFactory().Create(runtime, result);
        return new(1, input.Scenario.Id, input.Rules.RandomSeed, FastCombatEngine.TicksPerSecond,
            prepared, BattleSummary.From(resolved.CombatResult, input.Rules.MaxTicks),
            detailed ? result.EventLog : null);
    }

    public static CombatEncounterPlan CreatePlan(IdleBattleInput input) => new(
        StableRandom.Guid("balance-idle-v1", input.Scenario.Id,
            input.Rules.RandomSeed.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        CombatMode.Idle, 1, input.Scenario.StartsAt,
        [new("friendly-1", input.Character.Id, CombatSide.Friendly),
         .. input.Scenario.CreatureIds.Select((id, index) =>
             new CombatParticipantSlot(FormattableString.Invariant($"hostile-{index + 1}"), id, CombatSide.Hostile))],
        new IdleEncounterSourceContext(input.Character.Id, OfflineContent.ReadArea(input),
            TimeSpan.FromSeconds(input.EncounterCadenceSeconds)))
    {
        ContentType = CombatContentType.Idle,
        RandomSeed = input.Rules.RandomSeed,
        CaptureEventLog = true
    };

    public static JsonElement DescribeParticipants(CombatEncounterRuntime runtime) =>
        JsonSerializer.SerializeToElement(runtime.FriendlyParticipants.Concat(runtime.AllHostileParticipants)
            .Select(x => new
            {
                x.Slot, x.Combatant.Name, x.Combatant.Level, x.Combatant.SourceMonsterId,
                BaseAttributes = x.Combatant.BaseAttributes.ToDictionary(a => a.AttributeType, a => a.Value),
                x.Combatant.CombatAttributes,
                Health = x.Combatant.GetCurrentHealthValue(), Barrier = x.Combatant.GetCurrentBarrierValue(),
                Tags = x.Combatant.Tags.Order(StringComparer.Ordinal).ToArray(),
                NativeAbilityIds = x.Combatant.NativeAbilityIds.ToArray(),
                MainHand = x.Combatant.MainHandEquipment?.Id,
                OffHand = x.Combatant.OffHandEquipment?.Id,
                Equipment = x.Combatant.Equipment.OrderBy(e => e.Id).Select(e => e.ProgressionData).ToArray(),
                Essences = x.Combatant.EquippedEssences.Select(e => new
                    { e.EssenceDefinitionId, e.Level, e.AscensionTier, e.IsEvolved }).ToArray(),
                x.Combatant.TemporaryAbilityModifiers
            }).ToArray(), HarnessJson.Options);
}
