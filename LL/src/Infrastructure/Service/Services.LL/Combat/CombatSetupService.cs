using Application.Interfaces.Services.LL.Essences;
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

    public CombatSetupService(
        ICreatureScaler creatureScaler,
        IEssenceCombatLoadoutResolver essenceCombatLoadoutResolver,
        IEssenceDefinitionRepository essenceDefinitions)
    {
        _creatureScaler = creatureScaler;
        _essenceCombatLoadoutResolver = essenceCombatLoadoutResolver;
        _essenceDefinitions = essenceDefinitions;
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
                var monsterId = GetMonsterDefinitionId(creature);
                combatEntity.SourceMonsterId = monsterId;
                if (_essenceDefinitions.GetByMonsterId(monsterId) is { } essenceDefinition)
                    combatEntity.Tags = new HashSet<string>(essenceDefinition.Tags, StringComparer.OrdinalIgnoreCase);

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

    public async Task PrepareEntitiesForCombat(List<CombatEntity> entities)
    {
        foreach (var entity in entities)
        {
            var essenceLoadout = await ResolveEssenceLoadoutForCombatEntityAsync(entity);

            foreach (var modifier in essenceLoadout.AttributeModifiers)
            {
                if (entity.BaseAttributes.All(x => x.AttributeType != modifier.AttributeType))
                    entity.BaseAttributes.Add(new EntityAttribute { AttributeType = modifier.AttributeType, Value = 0 });

                entity.TemporaryModifiers.Add(modifier);
            }

            foreach (var tag in essenceLoadout.Tags)
                entity.Tags.Add(tag);

            entity.Abilities.AddRange(essenceLoadout.Abilities.Select(x => x.Ability));

            AttributeCalculator.CalculateBaseCombatAttributes(entity);
        }
    }

    public List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> combatEntities)
    {
        var simpleCombatEntities = new List<SimpleCombatEntity>();
        foreach (var entity in combatEntities)
        {
            simpleCombatEntities.Add(new SimpleCombatEntity(
                entity.Id,
                entity.Name,
                entity.ImagePath,
                (int)entity.BaseCombatAttributes[AttributeType.MaxHealth],
                (int)entity.BaseCombatAttributes[AttributeType.BlockEffectiveness])
            );
        }

        return simpleCombatEntities;
    }

    private static string GetMonsterDefinitionId(Creature creature) =>
        "monster." + creature.Name.Trim().Replace("'", "", StringComparison.Ordinal).Replace(" ", "_", StringComparison.Ordinal).ToLowerInvariant();

    private Task<EssenceCombatLoadout> ResolveEssenceLoadoutForCombatEntityAsync(CombatEntity entity)
    {
        if (entity.HasEquippedEssenceSnapshot)
            return Task.FromResult(_essenceCombatLoadoutResolver.Resolve(entity.OriginalId, entity.EquippedEssences));

        if (!string.IsNullOrWhiteSpace(entity.SourceMonsterId)
            && _essenceDefinitions.GetByMonsterId(entity.SourceMonsterId) is { } essenceDefinition)
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

        return _essenceCombatLoadoutResolver.ResolveAsync(entity.OriginalId, CancellationToken.None);
    }
}
