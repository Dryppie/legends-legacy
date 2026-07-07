using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates;
using Domain.Models.Entities.Creatures.Templates.Enums;
using Domain.Models.Regions.Areas;
using Services.LL.Entities.Creatures;

namespace EssenceSystem.Tests;

public sealed class CreatureScalerTests
{
    [Fact]
    public void ApplyScaling_keeps_first_area_creature_approachable()
    {
        var creature = CreateBalancedCreature();
        var area = new Area { Name = "Lumo Ruins", LevelRequirement = 1, DifficultyTier = 1 };

        new CreatureScaler().ApplyScaling(creature, area);

        Assert.Equal(60, creature.BaseAttributesDict[AttributeType.MaxHealth]);
        Assert.Equal(7, creature.BaseAttributesDict[AttributeType.Power]);
    }

    [Fact]
    public void ApplyScaling_still_increases_later_area_threat()
    {
        var firstAreaCreature = CreateBalancedCreature();
        var finalAreaCreature = CreateBalancedCreature();
        var scaler = new CreatureScaler();

        scaler.ApplyScaling(firstAreaCreature, new Area { Name = "Lumo Ruins", LevelRequirement = 1, DifficultyTier = 1 });
        scaler.ApplyScaling(finalAreaCreature, new Area { Name = "Forgotten Ruins", LevelRequirement = 45, DifficultyTier = 10 });

        Assert.True(finalAreaCreature.BaseAttributesDict[AttributeType.MaxHealth] >= firstAreaCreature.BaseAttributesDict[AttributeType.MaxHealth] * 3);
        Assert.True(finalAreaCreature.BaseAttributesDict[AttributeType.Power] >= firstAreaCreature.BaseAttributesDict[AttributeType.Power] * 2);
    }

    [Fact]
    public void ApplyScaling_applies_training_creature_stat_overrides()
    {
        var creature = CreateBalancedCreature();
        creature.StatOverrides =
        [
            new StatOverride { AttributeType = AttributeType.MaxHealth, Multiplier = 0.20f },
            new StatOverride { AttributeType = AttributeType.Power, Multiplier = 0.10f },
            new StatOverride { AttributeType = AttributeType.Precision, Multiplier = 0.50f }
        ];
        var area = new Area { Name = "Training Area", LevelRequirement = 1, DifficultyTier = 0 };

        new CreatureScaler().ApplyScaling(creature, area);

        Assert.Equal(12, creature.BaseAttributesDict[AttributeType.MaxHealth]);
        Assert.Equal(0, creature.BaseAttributesDict[AttributeType.Power]);
        Assert.Equal(5, creature.BaseAttributesDict[AttributeType.Precision]);
    }

    private static Creature CreateBalancedCreature() =>
        new()
        {
            Name = "Goblin",
            Archetype = CreatureArchetype.Balanced,
            DamageProfile = DamageProfile.Hybrid,
            DefenseProfile = DefenseProfile.Balanced
        };
}
