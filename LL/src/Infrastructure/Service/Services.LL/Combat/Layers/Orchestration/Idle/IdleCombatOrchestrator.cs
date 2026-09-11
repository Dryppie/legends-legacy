using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces.Combat.Orchestration;
using Services.LL.Interfaces.Combat.Resolution.Idle;
using Services.LL.Combat;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Services.LL.Spawnings;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.CombatStyles;
using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Bonuses;
using Domain.Models.Combat;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Extensions;
using Services.LL.Interfaces;

namespace Services.LL.Combat.Layers.Orchestration.Idle;

public sealed class IdleCombatOrchestrator : ICombatOrchestrator
{
    private readonly IIdleCombatPlanner _planner;
    private readonly IIdleCombatResolutionSessionFactory _resolutionSessionFactory;
    private readonly ICreatureArchiveService? _creatureArchive;
    private readonly ICombatStyleService? _combatStyles;
    private readonly IBonusService? _bonuses;
    private readonly IAreaExperienceBalanceProvider? _experienceBalance;
    private readonly Application.Interfaces.Services.LL.Nobility.INobilityService? _nobility;

    public IdleCombatOrchestrator(
        IIdleCombatPlanner planner,
        IIdleCombatResolutionSessionFactory resolutionSessionFactory,
        ICreatureArchiveService? creatureArchive = null,
        ICombatStyleService? combatStyles = null,
        IBonusService? bonuses = null,
        IAreaExperienceBalanceProvider? experienceBalance = null,
        Application.Interfaces.Services.LL.Nobility.INobilityService? nobility = null)
    {
        _planner = planner;
        _resolutionSessionFactory = resolutionSessionFactory;
        _creatureArchive = creatureArchive;
        _combatStyles = combatStyles;
        _bonuses = bonuses;
        _experienceBalance = experienceBalance;
        _nobility = nobility;
    }

    public CombatMode Mode => CombatMode.Idle;

    public async Task<CombatOrchestrationResult> OrchestrateAsync(
        CombatOrchestrationRequest request,
        CancellationToken cancellationToken)
    {
        if (request is not IdleCombatOrchestrationRequest idleRequest)
        {
            throw new ArgumentException(
                $"Expected {nameof(IdleCombatOrchestrationRequest)} but got {request.GetType().Name}.",
                nameof(request));
        }

        var plan = _planner.CreatePlan(idleRequest);
        if (_nobility is not null && plan.PlannedEncounterCount > 0)
        {
            var boundaries = new List<DateTimeOffset>();
            foreach (var recipient in plan.PlayerEntityIds.Distinct())
                boundaries.AddRange((await _nobility.GetCoverageAsync(recipient, cancellationToken))
                    .SelectMany(period => new[] { period.StartsAt, period.EndsAt }));
            var next = boundaries.Where(at => at > plan.From && at < plan.ExecutableUntil).Order().FirstOrDefault();
            if (next != default)
            {
                var count = checked((int)(((next - plan.From).Ticks + plan.EncounterCadence.Ticks - 1) / plan.EncounterCadence.Ticks));
                plan = plan with { PlannedEncounterCount = count, ExecutableUntil = plan.From.AddTicks(count * plan.EncounterCadence.Ticks) };
            }
        }
        using var entitlementTime = _nobility?.EvaluateCombatAt(plan.From);

        if (plan.PlannedEncounterCount == 0)
        {
            return CombatOrchestrationResults.None(CombatMode.Idle, new IdleCombatOrchestrationDetails(
                From: plan.From,
                RequestedTo: plan.RequestedTo,
                ProcessedUntil: plan.From,
                PlannedEncounterCount: plan.PlannedEncounterCount,
                EncounterCadence: plan.EncounterCadence));
        }

        var records = new List<CombatEncounterRecord>(plan.PlannedEncounterCount);
        var cursor = plan.From;

        var resolutionSession = await _resolutionSessionFactory.CreateAsync(
            plan,
            cancellationToken);

        var focusedCreature = _creatureArchive is null ? null
            : await _creatureArchive.GetCreatureFocusCreatureIdAsync(plan.CharacterId, cancellationToken);
        if (focusedCreature is not null)
        {
            var focusedIds = resolutionSession.SourceEntitiesById.Values.OfType<Creature>()
                .Where(creature => CreatureEssenceSource.GetMonsterDefinitionId(creature)
                    .Equals(focusedCreature, StringComparison.OrdinalIgnoreCase))
                .Select(creature => creature.Id).ToHashSet();
            plan = plan with
            {
                SpawnCreatures = WeightedSpawnSelector.ApplyCreatureFocus(plan.Area.Creatures.ToList(), focusedIds)
            };
        }

        var capturedStyles = resolutionSession.CapturedCombatStyles;
        var styleProgressionEnabled = capturedStyles.Count > 0 && _combatStyles is not null && _bonuses is not null && _experienceBalance is not null;
        var defeatRetention = styleProgressionEnabled
            ? (await _bonuses!.GetAggregatedAsync(plan.CharacterId, plan.RequestedTo, cancellationToken))
                .Get(BonusKind.IdleCombatDefeatExperienceRetentionBps)
            : 0;

        var simulationStartedAt = IdleCombatTelemetry.Start();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);

        for (var sequence = 1; sequence <= plan.PlannedEncounterCount; sequence++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var encounterPlan = _planner.CreateEncounterPlan(plan, sequence, cursor);
            var resolution = await resolutionSession.ResolveAsync(encounterPlan, cancellationToken);

            if (styleProgressionEnabled)
            {
                var baseXp = _experienceBalance!.CalculateEncounterExperience(plan.Area.Id, encounterPlan.HostileParticipants.Count);
                var eligibleXp = resolution.Outcome == BattleOutcome.Victory ? baseXp : baseXp.TakeBpsPortion(defeatRetention);
                var recipients = plan.PlayerEntityIds.Distinct().ToArray();
                var awards = new List<CombatStyleExperienceAward>();
                for (var index = 0; index < recipients.Length; index++)
                {
                    var recipient = recipients[index];
                    if (!capturedStyles.TryGetValue(recipient, out var captured)) continue;
                    var share = eligibleXp / recipients.Length + (index < eligibleXp % recipients.Length ? 1 : 0);
                    // The action cursor, character lock, and tracked progression commit in the same transaction.
                    // Advance templates only after this fight so offline batches match successive online encounters.
                    var grant = await _combatStyles!.GrantCapturedCombatXpAsync(recipient, captured.CombatStyleId, share, cancellationToken);
                    if (grant.LevelsGained > 0) resolutionSession.AdvanceCombatStyle(recipient, grant.Level);
                    awards.Add(new(recipient, captured.CombatStyleId, share, grant.XpGained, grant.Level));
                }
                resolution = resolution with { CombatStyleExperience = awards };
            }

            records.Add(new CombatEncounterRecord(encounterPlan, resolution));

            cursor = cursor.Add(plan.EncounterCadence);
        }

        IdleCombatTelemetry.RecordSimulation(simulationStartedAt, allocatedBefore);

        return new CombatOrchestrationResult(
            SessionId: Guid.NewGuid(),
            Mode: CombatMode.Idle,
            Encounters: records,
            Details: new IdleCombatOrchestrationDetails(
                From: plan.From,
                RequestedTo: plan.RequestedTo,
                ProcessedUntil: cursor,
                PlannedEncounterCount: plan.PlannedEncounterCount,
                EncounterCadence: plan.EncounterCadence),
            SourceEntitiesById: resolutionSession.SourceEntitiesById);
    }
}
