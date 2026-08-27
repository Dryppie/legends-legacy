using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.Combat.Resolution.Dungeon;

namespace Services.LL.Combat.Layers.Resolution.Dungeon;

public sealed class DungeonCombatResolutionSessionFactory : IDungeonCombatResolutionSessionFactory
{
    private readonly IEntityService _entityService;
    private readonly ICombatPreparationPipeline _combatPreparation;
    private readonly ICombatEngineExecutor _engineExecutor;
    private readonly ICombatEncounterResultFactory _resultFactory;

    public DungeonCombatResolutionSessionFactory(
        IEntityService entityService,
        ICombatPreparationPipeline combatPreparation,
        ICombatEngineExecutor engineExecutor,
        ICombatEncounterResultFactory resultFactory)
    {
        _entityService = entityService;
        _combatPreparation = combatPreparation;
        _engineExecutor = engineExecutor;
        _resultFactory = resultFactory;
    }

    public async Task<ICombatResolutionSession> CreateAsync(
        DungeonCombatPlan plan,
        CancellationToken cancellationToken)
    {
        var playerIds = plan.PlayerEntityIds
            .Distinct()
            .ToArray();

        var hostileIds = plan.EnemySourceEntityIds
            .ToArray();

        var entities = await _entityService.GetEntitiesByIdsForCombatAsync(
            [.. hostileIds.Distinct()],
            cancellationToken);

        var sourceEntitiesById = entities.ToDictionary(x => x.Id);

        var missingIds = hostileIds
            .Distinct()
            .Where(id => !sourceEntitiesById.ContainsKey(id))
            .ToArray();

        if (missingIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Failed to preload idle combat source entities. Missing: {string.Join(", ", missingIds)}");
        }

        var mismatchedPlayerIds = playerIds.Where(x => x != plan.CharacterSnapshot.CharacterId).ToArray();
        if (mismatchedPlayerIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Dungeon snapshot '{plan.CharacterSnapshot.Id}' belongs to character '{plan.CharacterSnapshot.CharacterId}', "
                + $"not: {string.Join(", ", mismatchedPlayerIds)}.");
        }

        var creatureArea = new Area
        {
            DifficultyTier = DungeonEnemyDifficultyScaling.GetProgressionPosition(
                plan.DungeonTier,
                plan.DungeonRegion)
        };
        var prepared = await _combatPreparation.PrepareAsync(
            CombatContentType.Dungeon,
            [
                .. playerIds.Select(playerId => new CombatantPreparationRequest(
                    new CombatParticipantSlot(
                        $"dungeon-template-{playerId:N}",
                        playerId,
                        CombatSide.Friendly),
                    new SnapshotCombatantPreparationSource(plan.CharacterSnapshot),
                    combatant => ApplyFriendlyModifiers(
                        combatant,
                        plan.RunAttributeModifiers,
                        plan.RunAbilityModifiers))),
                .. hostileIds.Distinct().Select(hostileId => new CombatantPreparationRequest(
                    new CombatParticipantSlot(
                        $"dungeon-template-hostile-{hostileId:N}",
                        hostileId,
                        CombatSide.Hostile),
                    new LiveCombatantPreparationSource(sourceEntitiesById[hostileId], creatureArea),
                    combatant => ApplyHostileModifiers(
                        combatant,
                        plan.DungeonTier,
                        plan.EnemyAttributeModifiers,
                        plan.EnemyStrengthMultiplier)))
            ],
            cancellationToken);
        foreach (var participant in prepared)
            sourceEntitiesById[participant.Slot.SourceEntityId] = participant.SourceEntity;
        var friendlyTemplates = prepared
            .Where(x => x.Slot.Side == CombatSide.Friendly)
            .ToDictionary(x => x.Slot.SourceEntityId, x => x.Combatant);
        var hostileTemplates = prepared
            .Where(x => x.Slot.Side == CombatSide.Hostile)
            .ToDictionary(x => x.Slot.SourceEntityId, x => x.Combatant);

        var catalog = new DungeonCombatTemplateCatalog(
            sourceEntitiesById,
            friendlyTemplates,
            hostileTemplates);

        return new DungeonCombatResolutionSession(
            _engineExecutor,
            _resultFactory)
        { Catalog = catalog };
    }

    private static void ApplyFriendlyModifiers(
        CombatEntity combatant,
        IReadOnlyList<Domain.Models.Attributes.Modifiers.AttributeModifierBase> runAttributeModifiers,
        IReadOnlyList<Domain.Models.Essences.Definitions.EssenceAbilityModifierDefinition> runAbilityModifiers)
    {
        AddAttributeModifiers(combatant, runAttributeModifiers);
        combatant.TemporaryAbilityModifiers.AddRange(runAbilityModifiers);
    }

    private static void ApplyHostileModifiers(
        CombatEntity combatant,
        int dungeonTier,
        IReadOnlyList<Domain.Models.Attributes.Modifiers.AttributeModifierBase> enemyAttributeModifiers,
        float? enemyStrengthMultiplier)
    {
        DungeonEnemyDifficultyScaling.Apply(combatant, dungeonTier, enemyStrengthMultiplier);
        AddAttributeModifiers(combatant, enemyAttributeModifiers);
    }

    private static void AddAttributeModifiers(
        CombatEntity combatant,
        IReadOnlyList<Domain.Models.Attributes.Modifiers.AttributeModifierBase> modifiers)
    {
        foreach (var modifier in modifiers)
        {
            if (combatant.BaseAttributes.All(x => x.AttributeType != modifier.AttributeType))
            {
                combatant.BaseAttributes.Add(new Domain.Models.Attributes.EntityAttribute
                {
                    AttributeType = modifier.AttributeType,
                    Value = 0
                });
            }

            combatant.TemporaryModifiers.Add(modifier);
        }
    }
}
