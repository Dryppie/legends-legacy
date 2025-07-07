using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.TierPackages;

namespace Persistence.LL.Repositories.Equipments;
public class InMemoryTierPackageProvider : ITierPackageProvider
{
    private readonly Random _rnd = new();
    public int Next(int min, int maxInclusive) => _rnd.Next(min, maxInclusive + 1);
    public T Pick<T>(T[] src) => src[_rnd.Next(src.Length)];

    private readonly IReadOnlyDictionary<Rarity, Func<TierPackage>> _factories;


    public TierPackage GetPackage(Rarity rarity)
    {
        if (!_factories.TryGetValue(rarity, out var factory))
            throw new ArgumentOutOfRangeException(nameof(rarity), rarity,
                "No package defined for this rarity");

        return factory();
    }

    public InMemoryTierPackageProvider()
    {;
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
    private TierPackage BuildRandomSingleStatPackage(Rarity rarity)
    {
        var attr = Pick(AttributeLists.PrimaryAttributes);
        var amount = Next(1, 15);

        var mod = new ItemAttributeModifier(attr, amount, ModifierType.Flat);
        return new TierPackage(rarity, [mod]);
    }
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