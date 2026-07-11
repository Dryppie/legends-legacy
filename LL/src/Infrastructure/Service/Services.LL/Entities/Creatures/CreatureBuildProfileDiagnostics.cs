using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Entities;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
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
    private readonly ICreatureRepository _creatures;

    public CreatureBuildProfileDiagnostics(
        ICreatureScaler creatureScaler,
        IEssenceDefinitionRepository essenceDefinitions,
        ICreatureRepository creatures)
    {
        _creatureScaler = creatureScaler;
        _essenceDefinitions = essenceDefinitions;
        _creatures = creatures;
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

    public async Task<CreatureBuildProfileDiagnosticReport> CreateReportAsync(CancellationToken cancellationToken)
    {
        var creatures = await _creatures.GetCreaturesAsync(cancellationToken);
        var diagnostics = new List<CreatureBuildProfileDiagnostic>();
        var warnings = new List<string>();
        var errors = new List<string>();

        foreach (var creature in creatures.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var area in RepresentativeAreas())
            {
                var diagnostic = Create(creature, area);
                diagnostics.Add(diagnostic);

                if (!diagnostic.EssenceDefinitionResolved)
                    warnings.Add($"{diagnostic.CreatureName} in {area.Name}: source '{diagnostic.SourceMonsterId}' does not resolve an Essence definition.");

                if (diagnostic.CombatRating <= 0)
                    errors.Add($"{diagnostic.CreatureName} in {area.Name}: generated Combat Rating is {diagnostic.CombatRating}.");

                if (diagnostic.FinalAttributes.GetValueOrDefault(AttributeType.MaxHealth) <= 0)
                    errors.Add($"{diagnostic.CreatureName} in {area.Name}: generated Max Health is missing or zero.");
            }
        }

        return new CreatureBuildProfileDiagnosticReport(
            creatures.Count,
            diagnostics.Count,
            diagnostics,
            warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            errors.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static IEnumerable<Area> RepresentativeAreas()
    {
        yield return CreateRepresentativeArea("diagnostic.area.tier_1", "Diagnostic Area Tier 1", 1);
        yield return CreateRepresentativeArea("diagnostic.area.tier_4", "Diagnostic Area Tier 4", 4);
        yield return CreateRepresentativeArea("diagnostic.area.tier_7", "Diagnostic Area Tier 7", 7);
    }

    private static Area CreateRepresentativeArea(string id, string name, int difficultyTier) =>
        new()
        {
            Id = id,
            Name = name,
            LevelRequirement = 1,
            DifficultyTier = difficultyTier
        };

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
            RewardTableId = creature.RewardTableId,
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
