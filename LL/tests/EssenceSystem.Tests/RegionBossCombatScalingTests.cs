using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Domain.Models.RegionBosses;
using Services.LL.RegionBosses;

namespace EssenceSystem.Tests;

public sealed class RegionBossCombatScalingTests
{
    [Fact]
    public void Level_one_uses_the_full_party_benchmark_and_flat_penetration()
    {
        var boss = Combatant();

        RegionBossCombatScaling.Apply(boss, Definition(), bossLevel: 1, partySize: 5);

        Assert.Equal(25_000, boss.GetAttributeValue(AttributeType.MaxHealth));
        Assert.Equal(2_000, boss.GetAttributeValue(AttributeType.Power));
        Assert.Equal(50, boss.GetAttributeValue(AttributeType.Armor));
        Assert.Equal(50, boss.GetAttributeValue(AttributeType.Resistance));
        Assert.Equal(18, boss.GetAttributeValue(AttributeType.ArmorPenetration));
        Assert.Equal(18, boss.GetAttributeValue(AttributeType.MagicPenetration));
    }

    [Fact]
    public void Later_levels_follow_the_authored_shifted_power_and_linear_curves()
    {
        var boss = Combatant();

        RegionBossCombatScaling.Apply(boss, Definition(), bossLevel: 6, partySize: 5);

        Assert.Equal(234_631, boss.GetAttributeValue(AttributeType.MaxHealth));
        Assert.Equal(6_139, boss.GetAttributeValue(AttributeType.Power));
        Assert.Equal(80, boss.GetAttributeValue(AttributeType.Armor));
        Assert.Equal(80, boss.GetAttributeValue(AttributeType.Resistance));
        Assert.Equal(27, boss.GetAttributeValue(AttributeType.ArmorPenetration));
        Assert.Equal(27, boss.GetAttributeValue(AttributeType.MagicPenetration));
    }

    [Fact]
    public void Smaller_parties_keep_most_of_the_offensive_pressure()
    {
        var fullPartyBoss = Combatant();
        var threePlayerBoss = Combatant();

        RegionBossCombatScaling.Apply(fullPartyBoss, Definition(), bossLevel: 1, partySize: 5);
        RegionBossCombatScaling.Apply(threePlayerBoss, Definition(), bossLevel: 1, partySize: 3);

        Assert.Equal(20_000, threePlayerBoss.GetAttributeValue(AttributeType.MaxHealth));
        Assert.Equal(1_800, threePlayerBoss.GetAttributeValue(AttributeType.Power));
        Assert.Equal(
            fullPartyBoss.GetAttributeValue(AttributeType.Armor),
            threePlayerBoss.GetAttributeValue(AttributeType.Armor));
        Assert.Equal(
            fullPartyBoss.GetAttributeValue(AttributeType.ArmorPenetration),
            threePlayerBoss.GetAttributeValue(AttributeType.ArmorPenetration));
    }

    private static CombatEntity Combatant()
    {
        var source = new Creature { Name = "Scaling target" };
        foreach (var attribute in new[]
                 {
                     AttributeType.MaxHealth,
                     AttributeType.Power,
                     AttributeType.HealthRegeneration
                 })
        {
            source.BaseCombatAttributes[attribute] = 100;
            source.CombatAttributes[attribute] = 100;
        }
        source.BaseCombatAttributes[AttributeType.Armor] = 10;
        source.BaseCombatAttributes[AttributeType.Resistance] = 10;
        source.CombatAttributes[AttributeType.Armor] = 10;
        source.CombatAttributes[AttributeType.Resistance] = 10;
        source.BaseCombatAttributes[AttributeType.ArmorPenetration] = 0;
        source.BaseCombatAttributes[AttributeType.MagicPenetration] = 0;
        source.CombatAttributes[AttributeType.ArmorPenetration] = 0;
        source.CombatAttributes[AttributeType.MagicPenetration] = 0;
        return new CombatEntity(source);
    }

    private static RegionBossDefinition Definition() => new()
    {
        BaseScaling = new RegionBossBaseScalingDefinition
        {
            Health = 250,
            Power = 20,
            Armor = 5,
            Resistance = 5,
            Penetration = 18,
            Regeneration = 1
        },
        LevelScaling = new RegionBossLevelScalingDefinition
        {
            GrowthCurve = RegionBossGrowthCurve.ShiftedPower,
            HealthGrowth = 0.75,
            HealthGrowthExponent = 1.50,
            PowerGrowth = 0.30,
            PowerGrowthExponent = 1.20,
            ArmorGrowthPerLevel = 0.12,
            ResistanceGrowthPerLevel = 0.12,
            PenetrationGrowthPerLevel = 0.10
        }
    };
}
