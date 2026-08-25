using Application.Interfaces.Services.LL.Combat;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Essences;
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
    ISnapshotCombatantBuilder snapshotCombatants,
    ICombatSetupService combatSetup) : IWorldTowerCombatRuntimeFactory
{
    public async Task<CombatEncounterRuntime> CreateAsync(
        WorldTowerCombatRuntimeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var friendly = (await snapshotCombatants.BuildAsync(
            request.FriendlyCombatants,
            cancellationToken)).ToList();
        foreach (var participant in friendly)
        {
            AddPercentModifier(
                participant.Combatant,
                AttributeType.Power,
                request.PlayerDamagePercent);
            AddPercentModifier(
                participant.Combatant,
                AttributeType.ArmorPenetration,
                request.WeakPointPercent);
            AddPercentModifier(
                participant.Combatant,
                AttributeType.MagicPenetration,
                request.WeakPointPercent);
        }

        var guardian = combatSetup.CreateCreatureCombatEntities(
            [request.GuardianSource],
            new Area { DifficultyTier = request.Definition.ProgressionPosition }).Single();
        WorldTowerGuardianScaling.Apply(
            guardian,
            request.Definition.GuardianScaling,
            request.Definition.RequiredSlots);
        guardian.StaggerDefinition = request.Definition.Stagger;
        guardian.StaggerParticipantCount = friendly.Count;
        AddPercentModifier(
            guardian,
            AttributeType.Power,
            -request.GuardianDamageReductionPercent);

        var hostileSlot = new CombatParticipantSlot(
            request.Definition.GuardianCreatureId.ToString(),
            request.Definition.GuardianCreatureId,
            CombatSide.Hostile);
        var hostile = new CombatRuntimeParticipant(
            hostileSlot,
            request.GuardianSource,
            guardian);

        await combatSetup.PrepareEntitiesForCombat(
            [.. friendly.Select(x => x.Combatant), guardian],
            EssenceCombatActivity.WorldTower);

        var plan = new CombatEncounterPlan(
            request.EncounterId,
            CombatMode.Raid,
            1,
            request.StartsAt,
            [.. friendly.Select(x => x.Slot), hostileSlot],
            new RaidEncounterSourceContext(
                request.RallyId,
                1,
                $"tower-floor-{request.Definition.FloorNumber}"));
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
