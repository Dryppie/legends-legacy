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
        ClampBarrier(creature);
        SyncHealth(creature);
        ClampCrits(creature);
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

    private static void ApplyDifficultyScaling(Creature creature, int D)
    {
        foreach (var type in MonsterBaseStats.Baseline.Keys)
        {
            var baseValue = MonsterBaseStats.Baseline[type];
            var scaled = baseValue;

            switch (type)
            {
                // Vitality
                case AttributeType.MaxHealth:
                    scaled = ScaleHp(baseValue, D);
                    break;

                case AttributeType.Health:
                    continue; // set from MaxHealth later

                case AttributeType.HealthRegeneration:
                case AttributeType.MaxMana:
                case AttributeType.Mana:
                case AttributeType.ManaRegeneration:
                case AttributeType.RecoveryRate:
                case AttributeType.Barrier:
                    break; // no D scaling

                // Offense
                case AttributeType.AttackPower:
                case AttributeType.SpellPower:
                    scaled = ScaleOffense(baseValue, D);
                    break;

                case AttributeType.AttackSpeed:
                    scaled = (float)(baseValue * (1.0 + MonsterScalingConstants.AttackSpeedPerTier * D));
                    break;

                case AttributeType.Accuracy:
                    scaled = (float)(baseValue * (1.0 + MonsterScalingConstants.AccuracyPerTier * D));
                    break;

                case AttributeType.CritChance:
                    {
                        var added = baseValue + (float)(MonsterScalingConstants.CritChancePerTier * D);
                        scaled = Math.Min(added, MonsterScalingConstants.CritChanceCap);
                        break;
                    }

                case AttributeType.CritDamage:
                    {
                        var added = baseValue + (float)(MonsterScalingConstants.CritDamagePerTier * D);
                        scaled = Math.Min(added, MonsterScalingConstants.CritDamageCap);
                        break;
                    }

                case AttributeType.MultiStrike:
                case AttributeType.MultiCast:
                    break; // role/traits only

                case AttributeType.ArmorPenetration:
                case AttributeType.ManaPenetration:
                    scaled = (float)(baseValue * (1.0 + MonsterScalingConstants.PenPerTier * D));
                    break;

                // Defense
                case AttributeType.PhysicalDefense:
                case AttributeType.MagicalDefense:
                    scaled = ScaleDefense(baseValue, D);
                    break;

                case AttributeType.DamageReduction:
                case AttributeType.CritDamageReduction:
                case AttributeType.CrowdControlResistance:
                    scaled = ScaleSoftDefense(baseValue, D);
                    break;

                case AttributeType.Dodge:
                case AttributeType.Block:
                case AttributeType.Parry:
                    break;

                // Control & utility
                case AttributeType.Threat:
                case AttributeType.CooldownReduction:
                    break;

                // Resistances
                case AttributeType.FireResistance:
                case AttributeType.WaterResistance:
                case AttributeType.EarthResistance:
                case AttributeType.AirResistance:
                    scaled = ScaleResistance(baseValue, D);
                    break;
            }

            creature.BaseAttributesDict[type] = scaled;
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

        ScaleGroup(creature, AttributeType.MaxHealth, AttributeType.Health, p.HealthMultiplier);
        ScaleGroup(creature, AttributeType.AttackPower, AttributeType.SpellPower, p.DamageMultiplier);
        ScaleGroup(creature, AttributeType.PhysicalDefense, AttributeType.MagicalDefense, p.DefenseMultiplier);
        ScaleSingle(creature, AttributeType.AttackSpeed, p.SpeedMultiplier);
    }

    private static void ApplyDamageProfile(Creature creature)
    {
        var p = DamageProfiles.Get(creature.DamageProfile);

        ScaleSingle(creature, AttributeType.AttackPower, p.PhysicalBias);
        ScaleSingle(creature, AttributeType.SpellPower, p.MagicalBias);
        ScaleSingle(creature, AttributeType.ArmorPenetration, p.PenBias);
        ScaleSingle(creature, AttributeType.ManaPenetration, p.PenBias);
        ScaleSingle(creature, AttributeType.CritChance, p.CritBias);
        ScaleSingle(creature, AttributeType.CritDamage, p.CritBias);
    }

    private static void ApplyDefenseProfile(Creature creature)
    {
        var p = DefenseProfiles.Get(creature.DefenseProfile);

        ScaleSingle(creature, AttributeType.PhysicalDefense, p.PhysicalDefenseBias);
        ScaleSingle(creature, AttributeType.MagicalDefense, p.MagicalDefenseBias);

        ScaleSingle(creature, AttributeType.FireResistance, p.ResistBias);
        ScaleSingle(creature, AttributeType.WaterResistance, p.ResistBias);
        ScaleSingle(creature, AttributeType.EarthResistance, p.ResistBias);
        ScaleSingle(creature, AttributeType.AirResistance, p.ResistBias);
    }

    //private static void ApplyElementProfile(Creature creature)
    //{
    //    var p = ElementProfiles.Get(creature.ElementProfileId);

    //    ScaleSingle(creature, AttributeType.FireResistance, p.FireResMultiplier);
    //    ScaleSingle(creature, AttributeType.WaterResistance, p.WaterResMultiplier);
    //    ScaleSingle(creature, AttributeType.EarthResistance, p.EarthResMultiplier);
    //    ScaleSingle(creature, AttributeType.AirResistance, p.AirResMultiplier);
    //}

    //private static void ApplyBossProfile(Creature creature)
    //{
    //    if (!creature.IsBoss || creature.BossRank == BossRank.None)
    //        return;

    //    var p = BossProfiles.Get(creature.BossRank);

    //    ScaleGroup(creature, AttributeType.MaxHealth, AttributeType.Health, p.HealthMultiplier);
    //    ScaleGroup(creature, AttributeType.AttackPower, AttributeType.SpellPower, p.DamageMultiplier);
    //    ScaleGroup(creature, AttributeType.PhysicalDefense, AttributeType.MagicalDefense, p.DefenseMultiplier);

    //    ScaleSingle(creature, AttributeType.AttackSpeed, p.SpeedMultiplier);
    //    ScaleSingle(creature, AttributeType.CooldownReduction, p.CdrMultiplier);
    //}

    //private static void ApplyCreatureFactors(Creature creature)
    //{
    //    ScaleGroup(creature, AttributeType.MaxHealth, AttributeType.Health, creature.HealthFactor);
    //    ScaleGroup(creature, AttributeType.AttackPower, AttributeType.SpellPower, creature.DamageFactor);
    //    ScaleGroup(creature, AttributeType.PhysicalDefense, AttributeType.MagicalDefense, creature.DefenseFactor);
    //    ScaleSingle(creature, AttributeType.AttackSpeed, creature.SpeedFactor);
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

    //            ScaleGroup(creature, AttributeType.MaxHealth, AttributeType.Health, ratio);
    //            ScaleSingle(creature, AttributeType.AttackPower, ratio);
    //            ScaleSingle(creature, AttributeType.SpellPower, ratio);
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

    //        ScaleGroup(creature, AttributeType.MaxHealth, AttributeType.Health, hpMult);
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

            creature.BaseAttributesDict[o.AttributeType] = val;
        }
    }

    private static void ClampBarrier(Creature creature)
    {
        if (!creature.BaseAttributesDict.TryGetValue(AttributeType.MaxHealth, out var hp))
            return;

        if (!creature.BaseAttributesDict.TryGetValue(AttributeType.Barrier, out var barrier))
            return;

        creature.BaseAttributesDict[AttributeType.Barrier] = Math.Min(barrier, hp * 2f);
    }

    private static void SyncHealth(Creature creature)
    {
        if (creature.BaseAttributesDict.TryGetValue(AttributeType.MaxHealth, out var maxHp))
        {
            creature.BaseAttributesDict[AttributeType.Health] = maxHp;
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

        c.BaseAttributesDict[type] = value * factor;
    }
}