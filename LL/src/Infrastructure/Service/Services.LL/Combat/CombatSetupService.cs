using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Combat;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Regions.Areas;
using Services.LL.Interfaces;

namespace Services.LL.Combat;

public class CombatSetupService : ICombatSetupService
{
    private readonly ICreatureScaler _creatureScaler;
    private readonly IEssenceCombatLoadoutResolver _essenceCombatLoadoutResolver;
    private readonly IEssenceDefinitionRepository _essenceDefinitions;
    private readonly ICreatureEssenceLootTableRepository _creatureEssenceLootTables;
    private readonly ICreatureAbilityDefinitionProvider? _creatureAbilities;

    public CombatSetupService(
        ICreatureScaler creatureScaler,
        IEssenceCombatLoadoutResolver essenceCombatLoadoutResolver,
        IEssenceDefinitionRepository essenceDefinitions,
        ICreatureEssenceLootTableRepository creatureEssenceLootTables,
        ICreatureAbilityDefinitionProvider? creatureAbilities = null)
    {
        _creatureScaler = creatureScaler;
        _essenceCombatLoadoutResolver = essenceCombatLoadoutResolver;
        _essenceDefinitions = essenceDefinitions;
        _creatureEssenceLootTables = creatureEssenceLootTables;
        _creatureAbilities = creatureAbilities;
    }

    public List<CombatEntity> CreatePlayerCombatEntities(List<Entity> entities)
    {
        var combatEntities = new List<CombatEntity>();
        foreach (var entity in entities)
        {
            var combatEntity = new CombatEntity(entity);
            combatEntities.Add(combatEntity);
        }
        return combatEntities;
    }

    public List<CombatEntity> CreateCreatureCombatEntities(List<Entity> entities, Area area)
    {
        var combatEntities = new List<CombatEntity>();
        foreach (var entity in entities)
        {
            if (entity is Creature creature)
            {
                _creatureScaler.ApplyScaling(creature, area);
                var combatEntity = new CombatEntity(creature)
                {
                    BaseAttributes = [.. creature.BaseAttributesDict
                    .Select(kv => new EntityAttribute
                    {
                        AttributeType = kv.Key,
                        Value = kv.Value
                    })],
                    Level = Math.Max(1, creature.Level > 1 ? creature.Level : creature.Tier)
                };
                var monsterId = CreatureEssenceSource.GetMonsterDefinitionId(creature);
                combatEntity.SourceMonsterId = monsterId;
                var authoredCreatureAbilities = _creatureAbilities?.GetAbilityIds(monsterId) ?? [];
                if (authoredCreatureAbilities.Count > 0)
                    combatEntity.NativeAbilityIds = [.. authoredCreatureAbilities];

                if (_creatureEssenceLootTables.GetByCreatureId(monsterId) is { } lootTable)
                {
                    if (combatEntity.NativeAbilityIds.Count == 0)
                    {
                        combatEntity.NativeAbilityIds = lootTable.Variants
                            .Select(x => x.ActiveAbilityId)
                            .Prepend(lootTable.PassiveAbilityId)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();
                    }
                    combatEntity.Tags = lootTable.Variants
                        .Select(x => _essenceDefinitions.GetById(x.EssenceDefinitionId))
                        .Where(x => x is not null)
                        .SelectMany(x => x!.Tags)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                }

                combatEntities.Add(combatEntity);
            }
        }
        return combatEntities;
    }

    public void AppendPrefixToId(List<CombatEntity> selectedCombatEnemyEntities)
    {
        var groupedEntities = selectedCombatEnemyEntities.GroupBy(e => e.Id);

        foreach (var group in groupedEntities)
        {
            var increment = 1;
            foreach (var entity in group)
            {
                entity.Id = $"{entity.Id}_{increment}";
                increment++;
            }
        }
    }

    public Task PrepareEntitiesForCombat(List<CombatEntity> entities) =>
        PrepareEntitiesForCombat(entities, EssenceCombatActivity.None);

    public async Task PrepareEntitiesForCombat(
        List<CombatEntity> entities,
        EssenceCombatActivity activity)
    {
        foreach (var entity in entities)
        {
            var essenceLoadout = await ResolveEssenceLoadoutForCombatEntityAsync(entity, activity);
            entity.EquippedEssences = [.. essenceLoadout.EquippedEssences];
            entity.HasEquippedEssenceSnapshot = entity.EquippedEssences.Count > 0;

            foreach (var modifier in essenceLoadout.AttributeModifiers)
            {
                if (entity.BaseAttributes.All(x => x.AttributeType != modifier.AttributeType))
                    entity.BaseAttributes.Add(new EntityAttribute { AttributeType = modifier.AttributeType, Value = 0 });

                entity.TemporaryModifiers.Add(modifier);
            }

            foreach (var tag in essenceLoadout.Tags)
                entity.Tags.Add(tag);

            AttributeCalculator.CalculateBaseCombatAttributes(entity);
        }
    }

    public List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> combatEntities)
    {
        var simpleCombatEntities = new List<SimpleCombatEntity>();
        foreach (var entity in combatEntities)
        {
            var simpleCombatEntity = new SimpleCombatEntity(
                entity.Id,
                entity.Name,
                entity.ImagePath,
                entity.GetAttributeValue(AttributeType.MaxHealth),
                entity.GetCurrentBarrierValue(),
                entity.Level)
            {
                Health = entity.GetCurrentHealthValue()
            };

            simpleCombatEntities.Add(simpleCombatEntity);
        }

        return simpleCombatEntities;
    }

    private Task<EssenceCombatLoadout> ResolveEssenceLoadoutForCombatEntityAsync(
        CombatEntity entity,
        EssenceCombatActivity activity)
    {
        if (entity.HasEquippedEssenceSnapshot)
            return Task.FromResult(_essenceCombatLoadoutResolver.Resolve(entity.OriginalId, entity.EquippedEssences));

        var firstVariant = string.IsNullOrWhiteSpace(entity.SourceMonsterId)
            ? null
            : _creatureEssenceLootTables.GetByCreatureId(entity.SourceMonsterId)?.Variants.FirstOrDefault();
        if (firstVariant is not null
            && _essenceDefinitions.GetById(firstVariant.EssenceDefinitionId) is { } essenceDefinition)
        {
            var monsterEssence = new PlayerEssence
            {
                Id = Guid.NewGuid(),
                CharacterId = entity.OriginalId,
                EssenceDefinitionId = essenceDefinition.Id,
                Level = Math.Max(1, entity.Level)
            };

            return Task.FromResult(_essenceCombatLoadoutResolver.Resolve(entity.OriginalId, [monsterEssence]));
        }

        if (!string.IsNullOrWhiteSpace(entity.SourceMonsterId))
        {
            return Task.FromResult(new EssenceCombatLoadout(
                entity.OriginalId,
                [],
                [],
                entity.Tags));
        }

        return _essenceCombatLoadoutResolver.ResolveAsync(entity.OriginalId, activity, CancellationToken.None);
    }
}
