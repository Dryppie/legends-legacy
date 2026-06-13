using Application.Interfaces.Services.LL.Essences;
using Domain.Components.Attributes;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates;
using Domain.Models.Essences;
using Domain.Models.Regions.Areas;
using Services.LL.Interfaces;

namespace Services.LL.Entities.Creatures;

public sealed class CreatureBuildProfileDiagnostics : ICreatureBuildProfileDiagnostics
{
    private readonly ICreatureScaler _creatureScaler;
    private readonly IEssenceDefinitionRepository _essenceDefinitions;

    public CreatureBuildProfileDiagnostics(
        ICreatureScaler creatureScaler,
        IEssenceDefinitionRepository essenceDefinitions)
    {
        _creatureScaler = creatureScaler;
        _essenceDefinitions = essenceDefinitions;
    }

    public CreatureBuildProfileDiagnostic Create(Creature creature, Area area)
    {
        var clone = CloneCreature(creature);
        _creatureScaler.ApplyScaling(clone, area);

        var sourceMonsterId = CreatureEssenceSource.GetMonsterDefinitionId(clone);
        var essenceDefinition = _essenceDefinitions.GetByMonsterId(sourceMonsterId);
        var finalAttributes = clone.BaseAttributesDict.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value);

        return new CreatureBuildProfileDiagnostic(
            clone.Id,
            clone.Name,
            sourceMonsterId,
            essenceDefinition is not null,
            essenceDefinition?.Id,
            Math.Max(1, area.DifficultyTier),
            clone.Archetype,
            clone.DamageProfile,
            clone.DefenseProfile,
            CombatRatingCalculator.Calculate(finalAttributes, clone.Level),
            finalAttributes);
    }

    private static Creature CloneCreature(Creature creature) =>
        new()
        {
            Id = creature.Id,
            Name = creature.Name,
            ImagePath = creature.ImagePath,
            Level = creature.Level,
            BaseLevel = creature.BaseLevel,
            Tier = creature.Tier,
            ExperienceReward = creature.ExperienceReward,
            LootTableId = creature.LootTableId,
            Archetype = creature.Archetype,
            DamageProfile = creature.DamageProfile,
            DefenseProfile = creature.DefenseProfile,
            StatOverrides = creature.StatOverrides
                .Select(CloneStatOverride)
                .ToList()
        };

    private static StatOverride CloneStatOverride(StatOverride statOverride) =>
        new()
        {
            Id = statOverride.Id,
            AttributeType = statOverride.AttributeType,
            Additive = statOverride.Additive,
            Multiplier = statOverride.Multiplier
        };
}
