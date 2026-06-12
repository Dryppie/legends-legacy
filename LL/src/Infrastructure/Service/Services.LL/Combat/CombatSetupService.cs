using Application.Interfaces.Services.LL.Essences;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;
using Services.LL.Interfaces;

namespace Services.LL.Combat;

public class CombatSetupService : ICombatSetupService
{
    private readonly ICreatureScaler _creatureScaler;
    private readonly IEssenceBonusProvider _essenceBonusProvider;
    private readonly IEssenceAbilityProvider _essenceAbilityProvider;
    private readonly IEssenceDefinitionRepository _essenceDefinitions;

    public CombatSetupService(
        ICreatureScaler creatureScaler,
        IEssenceBonusProvider essenceBonusProvider,
        IEssenceAbilityProvider essenceAbilityProvider,
        IEssenceDefinitionRepository essenceDefinitions)
    {
        _creatureScaler = creatureScaler;
        _essenceBonusProvider = essenceBonusProvider;
        _essenceAbilityProvider = essenceAbilityProvider;
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
                    })]
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
            var essenceModifiers = entity.HasEquippedEssenceSnapshot
                ? _essenceBonusProvider.GetAttunedAttributeModifiers(entity.EquippedEssences)
                : await _essenceBonusProvider.GetAttunedAttributeModifiersAsync(entity.OriginalId, CancellationToken.None);

            foreach (var modifier in essenceModifiers)
            {
                if (entity.BaseAttributes.All(x => x.AttributeType != modifier.AttributeType))
                    entity.BaseAttributes.Add(new EntityAttribute { AttributeType = modifier.AttributeType, Value = 0 });

                entity.TemporaryModifiers.Add(modifier);
            }

            var essenceAbilities = entity.HasEquippedEssenceSnapshot
                ? _essenceAbilityProvider.GetAttunedCombatAbilities(entity.EquippedEssences)
                : await _essenceAbilityProvider.GetAttunedCombatAbilitiesAsync(entity.OriginalId, CancellationToken.None);

            entity.Abilities.AddRange(essenceAbilities);

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
}
