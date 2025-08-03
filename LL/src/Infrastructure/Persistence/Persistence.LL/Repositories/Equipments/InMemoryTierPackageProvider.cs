using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.TierPackages;

namespace Persistence.LL.Repositories.Equipments;
public class InMemoryTierPackageProvider : ITierPackageProvider
{
    private readonly Random _rnd = new();

    public InMemoryTierPackageProvider()
    {
        _factories = new Dictionary<Rarity, Func<TierPackage>>
        {
            [Rarity.Uncommon] = () => BuildRandomSingleStatPackage(Rarity.Uncommon),
            [Rarity.Rare] = () => BuildRandomSingleStatPackage(Rarity.Rare),
            [Rarity.Epic] = () => BuildRandomSingleStatPackage(Rarity.Epic),
            [Rarity.Unique] = () => BuildRandomSingleStatPackage(Rarity.Unique),
            [Rarity.Legendary] = () => BuildRandomSingleStatPackage(Rarity.Legendary),
            [Rarity.Legacy] = () => BuildRandomSingleStatPackage(Rarity.Legacy),
        };
    }

    private readonly IReadOnlyDictionary<Rarity, Func<TierPackage>> _factories;

    public TierPackage GetPackage(Rarity rarity)
        => _factories.TryGetValue(rarity, out var f)
           ? f()
           : throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "No package for this rarity");

    // -----------------------------------------------------------------------
    private TierPackage BuildRandomSingleStatPackage(Rarity rarity, int itemLevel = 1)
    {
        var attr = Pick([.. EquipmentAttributeRules.Rules.Keys]);
        var rule = EquipmentAttributeRules.Rules[attr];

        var amount = Next(rule.Min, rule.Max);

        var mod = new InstanceAttributeModifier(attr, amount, rule.ModType);
        return new TierPackage(rarity, mod);
    }

    // -----------------------------------------------------------------------
    private int Next(int inclusiveMin, int inclusiveMax) =>
        _rnd.Next(inclusiveMin, inclusiveMax + 1);

    private AttributeType Pick(AttributeType[] src) =>
        src[_rnd.Next(src.Length)];
}

public static class AttributeLists
{
    public static readonly AttributeType[] PrimaryAttributes =
    {
        //AttributeType.Constitution,
        //AttributeType.Endurance,
        //AttributeType.Willpower,
        //AttributeType.Strength,
        //AttributeType.FightingSpirit,
        //AttributeType.Dexterity,
        //AttributeType.Agility,
        //AttributeType.Intelligence,
        //AttributeType.Wisdom,
        //AttributeType.Instinct,
        //AttributeType.Perception,
        //AttributeType.Luck
    };
}