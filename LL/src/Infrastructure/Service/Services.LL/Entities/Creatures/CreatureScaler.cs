using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates;
using Domain.Models.Regions.Areas;
using Services.LL.Interfaces;
using Services.LL.Regions;

namespace Services.LL.Entities.Creatures;

public class CreatureScaler : ICreatureScaler
{
    private readonly IRegionCreatureScalingProvider _scalingProvider;

    public CreatureScaler()
        : this(RegionCreatureScalingProvider.CreateLegacyFallback())
    {
    }

    public CreatureScaler(IRegionCreatureScalingProvider scalingProvider)
    {
        _scalingProvider = scalingProvider;
    }

    public void ApplyScaling(Creature creature, Area area)
    {
        var scaling = _scalingProvider.GetScaling(area);

        InitializeFromBaseline(creature);
        ApplyDifficultyScaling(creature, scaling);
        ApplyArchetype(creature);
        ApplyDamageProfile(creature);
        ApplyDefenseProfile(creature);
        ApplyStatOverrides(creature);
        SyncHealth(creature);
        ClampCrits(creature, scaling);
    }

    private static void InitializeFromBaseline(Creature creature)
    {
        creature.BaseCombatAttributes.Clear();
        creature.CombatAttributes.Clear(); // optional, but nice

        foreach (var kvp in MonsterBaseStats.Baseline)
        {
            creature.BaseAttributesDict[kvp.Key] = kvp.Value;
        }
    }

    private static void ApplyDifficultyScaling(
        Creature creature,
        CreatureScalingProfile scaling)
    {
        foreach (var type in MonsterBaseStats.Baseline.Keys)
        {
            var baseValue = MonsterBaseStats.Baseline[type];
            var scaled = type switch
            {
                AttributeType.MaxHealth => (float)(baseValue * scaling.HealthMultiplier),
                AttributeType.Power => (float)(baseValue * scaling.OffenseMultiplier),
                AttributeType.AttackSpeed => baseValue + (float)scaling.AttackSpeedBonus,
                AttributeType.CritChance => Math.Min(baseValue + (float)scaling.CritChanceBonus, scaling.CritChanceCap),
                AttributeType.CritDamage => Math.Min(baseValue + (float)scaling.CritDamageBonus, scaling.CritDamageCap),
                AttributeType.ArmorPenetration => baseValue + (float)scaling.PenetrationBonus,
                AttributeType.MagicPenetration => baseValue + (float)scaling.PenetrationBonus,
                AttributeType.Armor => (float)(baseValue * scaling.DefenseMultiplier),
                AttributeType.Resistance => (float)(baseValue * scaling.ResistanceMultiplier),
                AttributeType.HealthRegeneration => (float)(baseValue * Math.Sqrt(
                    scaling.HealthMultiplier * scaling.OffenseMultiplier)),
                AttributeType.DamageReduction => baseValue + (float)scaling.SoftDefenseBonus,
                AttributeType.CrowdControlResistance => baseValue + (float)scaling.SoftDefenseBonus,
                AttributeType.StatusResistance => baseValue + (float)scaling.SoftDefenseBonus,
                _ => baseValue
            };

            creature.BaseAttributesDict[type] = scaled;
        }
    }

    private static void ApplyArchetype(Creature creature)
    {
        var p = Archetypes.Get(creature.Archetype);

        ScaleSingle(creature, AttributeType.MaxHealth, p.HealthMultiplier);
        ScaleSingle(creature, AttributeType.Power, p.DamageMultiplier);
        ScaleGroup(creature, AttributeType.Armor, AttributeType.Resistance, p.DefenseMultiplier);
        ScaleSingle(creature, AttributeType.AttackSpeed, p.SpeedMultiplier);
    }

    private static void ApplyDamageProfile(Creature creature)
    {
        var p = DamageProfiles.Get(creature.DamageProfile);

        ScaleSingle(creature, AttributeType.Power, (p.PhysicalBias + p.MagicalBias) / 2f);
        ScaleGroup(creature, AttributeType.ArmorPenetration, AttributeType.MagicPenetration, p.PenBias);
        ScaleSingle(creature, AttributeType.CritChance, p.CritBias);
        ScaleSingle(creature, AttributeType.CritDamage, p.CritBias);
    }

    private static void ApplyDefenseProfile(Creature creature)
    {
        var p = DefenseProfiles.Get(creature.DefenseProfile);

        ScaleSingle(creature, AttributeType.Armor, p.PhysicalDefenseBias);
        ScaleSingle(creature, AttributeType.Resistance, p.MagicalDefenseBias);
        ScaleSingle(creature, AttributeType.Resistance, p.ResistBias);
    }

    private static void ApplyStatOverrides(Creature creature)
    {
        foreach (var o in creature.StatOverrides)
        {
            if (!creature.BaseAttributesDict.TryGetValue(o.AttributeType, out var val))
                continue;

            if (o.Multiplier.HasValue)
                val *= o.Multiplier.Value;
            if (o.Additive.HasValue)
                val += o.Additive.Value;

            creature.BaseAttributesDict[o.AttributeType] = val;
        }
    }

    private static void SyncHealth(Creature creature)
    {
        if (creature.BaseAttributesDict.TryGetValue(AttributeType.MaxHealth, out var maxHp))
        {
            creature.BaseAttributesDict[AttributeType.MaxHealth] = maxHp;
        }
    }

    private static void ClampCrits(Creature creature, CreatureScalingProfile scaling)
    {
        if (creature.BaseAttributesDict.TryGetValue(AttributeType.CritChance, out var cc))
        {
            creature.BaseAttributesDict[AttributeType.CritChance] =
                Math.Clamp(cc, 0f, scaling.CritChanceCap);
        }

        if (creature.BaseAttributesDict.TryGetValue(AttributeType.CritDamage, out var cd))
        {
            creature.BaseAttributesDict[AttributeType.CritDamage] =
                Math.Clamp(cd, 0f, scaling.CritDamageCap);
        }
    }

    private static void ScaleGroup(Creature c, AttributeType a, AttributeType b, float factor)
    {
        ScaleSingle(c, a, factor);
        ScaleSingle(c, b, factor);
    }

    private static void ScaleSingle(Creature c, AttributeType type, float factor)
    {
        if (!c.BaseAttributesDict.TryGetValue(type, out var value))
            return;

        c.BaseAttributesDict[type] = value * factor;
    }
}
