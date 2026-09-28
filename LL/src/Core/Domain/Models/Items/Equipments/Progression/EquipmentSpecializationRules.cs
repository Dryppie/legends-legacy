using Domain.Models.Attributes;

namespace Domain.Models.Items.Equipments.Progression;

public static class EquipmentSpecializationRules
{
    public const double CoreShare = .7d;
    public const double SpecializationShare = .3d;
    public const double IdentityShare = .1d;

    public static bool IsAllowed(EquipmentType slot, AttributeType attribute) => slot switch
    {
        EquipmentType.Head => attribute is AttributeType.Tenacity or AttributeType.HealthRegeneration or AttributeType.Armor or AttributeType.Resistance or AttributeType.Restoration,
        EquipmentType.Chest => attribute is AttributeType.MaxHealth or AttributeType.Armor or AttributeType.Resistance or AttributeType.BlockChance or AttributeType.Restoration,
        EquipmentType.Legs => attribute is AttributeType.Tenacity or AttributeType.HealthRegeneration or AttributeType.Armor or AttributeType.Resistance,
        EquipmentType.Necklace => attribute is AttributeType.Tenacity or AttributeType.HealthRegeneration or AttributeType.MaxHealth or AttributeType.Restoration,
        EquipmentType.Relic => attribute is AttributeType.AbilityHaste or AttributeType.Restoration or AttributeType.Tenacity,
        EquipmentType.Ring => attribute is AttributeType.CritChance or AttributeType.CritDamage or AttributeType.ArmorPenetration or AttributeType.MagicPenetration or AttributeType.Restoration,
        EquipmentType.OneHanded or EquipmentType.TwoHanded => attribute is AttributeType.AttackSpeed or AttributeType.CritChance or AttributeType.CritDamage or AttributeType.ArmorPenetration or AttributeType.MagicPenetration or AttributeType.Restoration or AttributeType.AbilityHaste,
        EquipmentType.OffHand => attribute is AttributeType.BlockChance or AttributeType.Tenacity or AttributeType.Armor or AttributeType.Resistance or AttributeType.AbilityHaste or AttributeType.Restoration or AttributeType.MagicPenetration,
        _ => false
    };

    public static void Validate(EquipmentArchetype archetype, IReadOnlyDictionary<AttributeType, double> specialty)
    {
        if (archetype.StatWeights.Keys.Any(attribute => attribute is not (AttributeType.Power or AttributeType.MaxHealth or AttributeType.Armor or AttributeType.Resistance)
            && !(archetype.EquipmentType == EquipmentType.Relic && attribute == AttributeType.HealthRegeneration)))
            throw new ArgumentException($"Invalid core allocation for '{archetype.Id}'.");
        if (specialty.Count == 0 || specialty.Keys.Any(attribute => !IsAllowed(archetype.EquipmentType, attribute)))
            throw new ArgumentException($"Invalid specialization for '{archetype.Id}'.");
        ValidateCombination(specialty.Keys);
    }

    public static void ValidateCombination(IEnumerable<AttributeType> attributes)
    {
        var set = attributes.ToHashSet();
        if (set.Contains(AttributeType.AttackSpeed) && set.Contains(AttributeType.AbilityHaste))
            throw new ArgumentException("One item cannot grant both Attack Speed and Ability Haste.");
        if (set.Contains(AttributeType.CritDamage) && !set.Contains(AttributeType.CritChance))
            throw new ArgumentException("Critical damage requires a paired critical chance allocation.");
    }

    public static IReadOnlyDictionary<AttributeType, double> CompatibleStyle(
        EquipmentArchetype archetype, EquipmentStyle style, IEnumerable<AttributeType> specialty)
    {
        var chosen = specialty.ToHashSet();
        var result = style.StatWeights.Where(x => AttributeRules.IsOrdinaryEquipmentAttribute(x.Key)
            && (archetype.StatWeights.ContainsKey(x.Key) || IsAllowed(archetype.EquipmentType, x.Key))
            && !(x.Key == AttributeType.AttackSpeed && chosen.Contains(AttributeType.AbilityHaste))
            && !(x.Key == AttributeType.AbilityHaste && chosen.Contains(AttributeType.AttackSpeed)))
            .ToDictionary(x => x.Key, x => x.Value);
        if (result.ContainsKey(AttributeType.CritDamage) && !chosen.Contains(AttributeType.CritChance) && !result.ContainsKey(AttributeType.CritChance))
        {
            var budget = result[AttributeType.CritDamage];
            result[AttributeType.CritChance] = budget * .6;
            result[AttributeType.CritDamage] = budget * .4;
        }
        // The base's legal specialization is an explicit fallback, never a removed or forbidden stat.
        return result.Count > 0 ? result : chosen.ToDictionary(x => x, _ => 1d);
    }
}

public sealed record EquipmentBudgetBreakdown(
    int StatVersion, double Core, double Specialization, double StyleStats, double ReservedIdentity,
    string SpecializationId)
{
    public double Total => Core + Specialization + StyleStats + ReservedIdentity;
}
