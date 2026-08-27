using Application.Interfaces.Services.LL.Combat;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.WorldTower;

namespace Services.LL.WorldTower;

/// <summary>
/// The single preparation path for live and calibrated World Tower combat.
/// Snapshot rehydration, gear/Essence preparation, guardian scaling, party
/// identity, and encounter metadata must remain identical for both callers.
/// </summary>
public sealed class WorldTowerCombatRuntimeFactory(
    ICombatPreparationPipeline combatPreparation) : IWorldTowerCombatRuntimeFactory
{
    public async Task<CombatEncounterRuntime> CreateAsync(
        WorldTowerCombatRuntimeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hostileSlot = new CombatParticipantSlot(
            request.Definition.GuardianCreatureId.ToString(),
            request.Definition.GuardianCreatureId,
            CombatSide.Hostile);
        var preparationRequests = request.FriendlyCombatants
            .Select(snapshotRequest => new CombatantPreparationRequest(
                snapshotRequest.Slot,
                new SnapshotCombatantPreparationSource(snapshotRequest.Snapshot),
                combatant =>
                {
                    AddPercentModifier(
                        combatant,
                        AttributeType.Power,
                        request.PlayerDamagePercent);
                    AddPercentModifier(
                        combatant,
                        AttributeType.ArmorPenetration,
                        request.WeakPointPercent);
                    AddPercentModifier(
                        combatant,
                        AttributeType.MagicPenetration,
                        request.WeakPointPercent);
                }))
            .Append(new CombatantPreparationRequest(
                hostileSlot,
                new LiveCombatantPreparationSource(
                    request.GuardianSource,
                    new Area { DifficultyTier = request.Definition.ProgressionPosition }),
                combatant =>
                {
                    WorldTowerGuardianScaling.Apply(
                        combatant,
                        request.Definition.GuardianScaling,
                        request.Definition.RequiredSlots);
                    combatant.StaggerDefinition = request.Definition.Stagger;
                    combatant.StaggerParticipantCount = request.FriendlyCombatants.Count;
                    AddPercentModifier(
                        combatant,
                        AttributeType.Power,
                        -request.GuardianDamageReductionPercent);
                }))
            .ToArray();
        var participants = await combatPreparation.PrepareAsync(
            CombatContentType.WorldTower,
            preparationRequests,
            cancellationToken);
        var friendly = participants.Where(x => x.Slot.Side == CombatSide.Friendly).ToList();
        var hostile = participants.Single(x => x.Slot.Side == CombatSide.Hostile);

        var plan = new CombatEncounterPlan(
            request.EncounterId,
            CombatMode.Raid,
            1,
            request.StartsAt,
            [.. friendly.Select(x => x.Slot), hostileSlot],
            new RaidEncounterSourceContext(
                request.RallyId,
                1,
                $"tower-floor-{request.Definition.FloorNumber}"))
        {
            ContentType = CombatContentType.WorldTower,
            RandomSeed = request.RandomSeed
        };
        return new CombatEncounterRuntime(plan, friendly, [hostile]);
    }

    private static void AddPercentModifier(
        Domain.Models.Combat.CombatEntity entity,
        AttributeType attribute,
        decimal amount)
    {
        if (amount == 0)
            return;
        entity.TemporaryModifiers.Add(new InstanceAttributeModifier(
            attribute,
            (float)amount,
            ModifierType.Multiplicative));
    }
}
