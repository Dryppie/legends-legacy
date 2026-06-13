using Domain.Helpers.Constants;
using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates;
using Domain.Models.Regions.Areas;
using Services.LL.Interfaces;

namespace Services.LL.Entities.Creatures;

public class CreatureScaler : ICreatureScaler
{
    public void ApplyScaling(Creature creature, Area area)
    {
        var D = Math.Max(1, area.DifficultyTier);

        InitializeFromBaseline(creature);
        ApplyDifficultyScaling(creature, D);
        ApplyArchetype(creature);
        ApplyDamageProfile(creature);
        ApplyDefenseProfile(creature);
        //ApplyElementProfile(creature);
        //ApplyBossProfile(creature);
        //ApplyCreatureFactors(creature);
        //ApplyRubberBanding(creature, area, D);
        ApplyStatOverrides(creature);
        SyncHealth(creature);
        ClampCrits(creature);
    }

    private static void InitializeFromBaseline(Creature creature)
    {
        creature.BaseCombatAttributes.Clear();
        creature.CombatAttributes.Clear(); // optional, but nice

        foreach (var kvp in MonsterBaseStats.Baseline)
        {
            creature.BaseAttributesDict[kvp.Key] = (int)kvp.Value;
        }
    }

    private static void ApplyDifficultyScaling(Creature creature, int D)
    {
        foreach (var type in MonsterBaseStats.Baseline.Keys)
        {
            var baseValue = MonsterBaseStats.Baseline[type];
            var scaled = type switch
            {
                AttributeType.MaxHealth => ScaleHp(baseValue, D),
                AttributeType.Power => ScaleOffense(baseValue, D),
                AttributeType.Precision => (float)(baseValue * (1.0 + MonsterScalingConstants.AccuracyPerTier * D)),
                AttributeType.CritChance => Math.Min(baseValue + (float)(MonsterScalingConstants.CritChancePerTier * D), MonsterScalingConstants.CritChanceCap),
                AttributeType.CritDamage => Math.Min(baseValue + (float)(MonsterScalingConstants.CritDamagePerTier * D), MonsterScalingConstants.CritDamageCap),
                AttributeType.ArmorPenetration => (float)(baseValue * (1.0 + MonsterScalingConstants.PenPerTier * D)),
                AttributeType.MagicPenetration => (float)(baseValue * (1.0 + MonsterScalingConstants.PenPerTier * D)),
                AttributeType.Armor => ScaleDefense(baseValue, D),
                AttributeType.Resistance => ScaleResistance(baseValue, D),
                AttributeType.DamageReduction => ScaleSoftDefense(baseValue, D),
                AttributeType.CrowdControlResistance => ScaleSoftDefense(baseValue, D),
                AttributeType.StatusResistance => ScaleSoftDefense(baseValue, D),
                _ => baseValue
            };

            creature.BaseAttributesDict[type] = (int)scaled;
        }
    }

    private static float ScaleHp(float baseHp, int D)
    {
        var mult = Math.Pow(1 + MonsterScalingConstants.HpA * D, MonsterScalingConstants.HpB);
        return (float)(baseHp * mult);
    }

    private static float ScaleOffense(float baseVal, int D)
    {
        var mult = Math.Pow(1 + MonsterScalingConstants.OffenseC * D, MonsterScalingConstants.OffenseExp);
        return (float)(baseVal * mult);
    }

    private static float ScaleDefense(float baseVal, int D)
    {
        if (baseVal <= 0) return baseVal;
        var mult = Math.Pow(1 + MonsterScalingConstants.DefenseA * D, MonsterScalingConstants.DefenseB);
        return (float)(baseVal * mult);
    }

    private static float ScaleSoftDefense(float baseVal, int D)
    {
        if (baseVal <= 0) return baseVal;
        var mult = 1.0 + 0.05 * D;
        return (float)(baseVal * mult);
    }

    private static float ScaleResistance(float baseVal, int D)
    {
        if (baseVal <= 0) return baseVal;
        var mult = Math.Pow(1 + MonsterScalingConstants.ResistA * D, MonsterScalingConstants.ResistB);
        return (float)(baseVal * mult);
    }

    private static void ApplyArchetype(Creature creature)
    {
        var p = Archetypes.Get(creature.Archetype);

        ScaleSingle(creature, AttributeType.MaxHealth, p.HealthMultiplier);
        ScaleSingle(creature, AttributeType.Power, p.DamageMultiplier);
        ScaleGroup(creature, AttributeType.Armor, AttributeType.Resistance, p.DefenseMultiplier);
        ScaleSingle(creature, AttributeType.Precision, p.SpeedMultiplier);
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

    //private static void ApplyElementProfile(Creature creature)
    //{
    //    var p = ElementProfiles.Get(creature.ElementProfileId);

    //    ScaleSingle(creature, AttributeType.Resistance, p.FireResMultiplier);
    //    ScaleSingle(creature, AttributeType.Resistance, p.WaterResMultiplier);
    //    ScaleSingle(creature, AttributeType.Resistance, p.EarthResMultiplier);
    //    ScaleSingle(creature, AttributeType.Resistance, p.AirResMultiplier);
    //}

    //private static void ApplyBossProfile(Creature creature)
    //{
    //    if (!creature.IsBoss || creature.BossRank == BossRank.None)
    //        return;

    //    var p = BossProfiles.Get(creature.BossRank);

    //    ScaleGroup(creature, AttributeType.MaxHealth, AttributeType.MaxHealth, p.HealthMultiplier);
    //    ScaleGroup(creature, AttributeType.Power, AttributeType.Power, p.DamageMultiplier);
    //    ScaleGroup(creature, AttributeType.Armor, AttributeType.Resistance, p.DefenseMultiplier);

    //    ScaleSingle(creature, AttributeType.Precision, p.SpeedMultiplier);
    //    ScaleSingle(creature, AttributeType.Cooldown, p.CdrMultiplier);
    //}

    //private static void ApplyCreatureFactors(Creature creature)
    //{
    //    ScaleGroup(creature, AttributeType.MaxHealth, AttributeType.MaxHealth, creature.HealthFactor);
    //    ScaleGroup(creature, AttributeType.Power, AttributeType.Power, creature.DamageFactor);
    //    ScaleGroup(creature, AttributeType.Armor, AttributeType.Resistance, creature.DefenseFactor);
    //    ScaleSingle(creature, AttributeType.Precision, creature.SpeedFactor);
    //}

    //private static void ApplyRubberBanding(Creature creature, Area ctx, int effectiveD)
    //{
    //    var ps = ctx.PlayerPowerScore;
    //    var min = ctx.Area.TargetPsMin;
    //    var max = ctx.Area.TargetPsMax;

    //    if (ps <= 0 || min <= 0 || max <= 0)
    //        return;

    //    // Overgeared → cap effective difficulty for HP + damage
    //    if (ps > max)
    //    {
    //        var clampedD = ctx.Area.DifficultyTier + MonsterScalingConstants.OvergearedClampDeltaD;
    //        if (effectiveD > clampedD)
    //        {
    //            var ratio = (float)clampedD / effectiveD;

    //            ScaleGroup(creature, AttributeType.MaxHealth, AttributeType.MaxHealth, ratio);
    //            ScaleSingle(creature, AttributeType.Power, ratio);
    //            ScaleSingle(creature, AttributeType.Power, ratio);
    //        }

    //        return;
    //    }

    //    // Undergeared → reduce HP only
    //    if (ps < min)
    //    {
    //        var weaknessFactor = (float)ps / min;
    //        var hpMult = Math.Max(
    //            (float)MonsterScalingConstants.UndergearedMinTtkHpMultiplier,
    //            weaknessFactor
    //        );

    //        ScaleGroup(creature, AttributeType.MaxHealth, AttributeType.MaxHealth, hpMult);
    //    }
    //}

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

            creature.BaseAttributesDict[o.AttributeType] = (int)val;
        }
    }

    private static void SyncHealth(Creature creature)
    {
        if (creature.BaseAttributesDict.TryGetValue(AttributeType.MaxHealth, out var maxHp))
        {
            creature.BaseAttributesDict[AttributeType.MaxHealth] = maxHp;
        }
    }

    private static void ClampCrits(Creature creature)
    {
        if (creature.BaseAttributesDict.TryGetValue(AttributeType.CritChance, out var cc))
        {
            creature.BaseAttributesDict[AttributeType.CritChance] =
                Math.Clamp(cc, 0f, MonsterScalingConstants.CritChanceCap);
        }

        if (creature.BaseAttributesDict.TryGetValue(AttributeType.CritDamage, out var cd))
        {
            creature.BaseAttributesDict[AttributeType.CritDamage] =
                Math.Clamp(cd, 1f, MonsterScalingConstants.CritDamageCap);
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

        c.BaseAttributesDict[type] = (int)(value * factor);
    }
}
