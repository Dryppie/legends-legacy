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
    public void ApplyScaling_applies_training_creature_stat_overrides()
    {
        var creature = CreateBalancedCreature();
        creature.StatOverrides =
        [
            new StatOverride { AttributeType = AttributeType.MaxHealth, Multiplier = 0.20f },
            new StatOverride { AttributeType = AttributeType.Power, Multiplier = 0.10f },
            new StatOverride { AttributeType = AttributeType.Armor, Multiplier = 0.50f }
        ];
        var area = new Area { Name = "Training Area", LevelRequirement = 1, DifficultyTier = 0 };

        new CreatureScaler().ApplyScaling(creature, area);

        Assert.Equal(12, creature.BaseAttributesDict[AttributeType.MaxHealth]);
        Assert.Equal(0, creature.BaseAttributesDict[AttributeType.Power]);
        Assert.Equal(5, creature.BaseAttributesDict[AttributeType.Armor]);
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
