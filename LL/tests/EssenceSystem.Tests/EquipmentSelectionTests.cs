using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class EquipmentSelectionTests
{
    private readonly StarterEquipmentCatalog _equipment = JsonStarterEquipmentCatalog.Load(
        Path.Combine(TestContentPaths.FindApiRoot(), "Data/equipment/equipment-starters.v1.json"));
    private static readonly EquipmentSelectionWeights Weights = new(0.4, 0.35, 0.25, 0.6, 0.4);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Category_and_handedness_shares_are_independent_of_catalog_size(bool expandWeapons)
    {
        var definitions = BaseDefinitions();
        if (expandWeapons)
            definitions = definitions.Concat(Enumerable.Range(0, 100).Select(index => new EquipmentDefinition(
                $"additional.weapon.{index}", $"Additional Weapon {index}", "plain.greatsword", EquipmentRarity.Common))).ToArray();

        var counts = new Dictionary<string, int>();
        // Stratified rolls cover each percentile of both independent distributions.
        for (var category = 0; category < 100; category++)
        for (var handedness = 0; handedness < 100; handedness++)
        {
            var selected = Weights.Roll(definitions, _equipment.Evaluator,
                new FixedRandom((category + 0.5) / 100d, (handedness + 0.5) / 100d));
            var group = Group(_equipment.Evaluator.GetArchetype(selected.ArchetypeId).EquipmentType);
            counts[group] = counts.GetValueOrDefault(group) + 1;
        }

        Assert.Equal(2400, counts["one-handed"]);
        Assert.Equal(1600, counts["two-handed"]);
        Assert.Equal(3500, counts["armor"]);
        Assert.Equal(2500, counts["jewelry"]);
    }

    [Fact]
    public void Category_and_handedness_boundaries_select_the_next_positive_weight_group()
    {
        var cases = new[]
        {
            (0d, 0d, "one-handed"),
            (Math.BitDecrement(0.4), Math.BitDecrement(0.6), "one-handed"),
            (0d, 0.6, "two-handed"),
            (Math.BitDecrement(0.4), Math.BitDecrement(1d), "two-handed"),
            (0.4, 0d, "armor"),
            (Math.BitDecrement(0.75), 0d, "armor"),
            (0.75, 0d, "jewelry"),
            (Math.BitDecrement(1d), 0d, "jewelry")
        };
        foreach (var (category, handedness, expected) in cases)
        {
            var selected = Weights.Roll(BaseDefinitions(), _equipment.Evaluator, new FixedRandom(category, handedness));
            Assert.Equal(expected, Group(_equipment.Evaluator.GetArchetype(selected.ArchetypeId).EquipmentType));
        }
    }

    [Fact]
    public void Every_one_handed_weapon_and_off_hand_item_shares_the_same_uniform_item_roll()
    {
        var definitions = BaseDefinitions();
        var expected = definitions.Where(definition =>
            _equipment.Evaluator.GetArchetype(definition.ArchetypeId).EquipmentType
                is EquipmentType.OneHanded or EquipmentType.OffHand).ToArray();
        var selected = Enumerable.Range(0, expected.Length).Select(index =>
            Weights.Roll(definitions, _equipment.Evaluator, new FixedRandom(0, 0, index, expected.Length))).ToArray();

        Assert.Equal(expected.Select(definition => definition.Id).Order(), selected.Select(definition => definition.Id).Order());
        Assert.Contains(selected, definition => definition.ArchetypeId == "plain.towershield");
        Assert.Contains(selected, definition => definition.ArchetypeId == "plain.spiritward");
        Assert.Contains(selected, definition => definition.ArchetypeId == "plain.grimoire");
    }

    [Theory]
    [InlineData(EquipmentType.OneHanded)]
    [InlineData(EquipmentType.TwoHanded)]
    [InlineData(EquipmentType.OffHand)]
    public void Restricted_pools_renormalize_categories_without_reducing_weapons_for_a_missing_handedness(EquipmentType weaponType)
    {
        var definitions = BaseDefinitions().Where(definition =>
            _equipment.Evaluator.GetArchetype(definition.ArchetypeId).EquipmentType == weaponType
            || _equipment.Evaluator.GetArchetype(definition.ArchetypeId).EquipmentType == EquipmentType.Head).ToArray();

        // With no jewelry, weapons have 40/75 of the drops, even with only one handedness.
        var weapon = Weights.Roll(definitions, _equipment.Evaluator, new FixedRandom(0.53, 0.99));
        var armor = Weights.Roll(definitions, _equipment.Evaluator, new FixedRandom(0.54));
        Assert.Equal(weaponType, _equipment.Evaluator.GetArchetype(weapon.ArchetypeId).EquipmentType);
        Assert.Equal(EquipmentType.Head, _equipment.Evaluator.GetArchetype(armor.ArchetypeId).EquipmentType);
    }

    [Fact]
    public void Zero_weight_groups_are_excluded_and_empty_eligible_pools_fail_explicitly()
    {
        var weights = new EquipmentSelectionWeights(1, 0, 0, 0, 1);
        weights.Validate();
        var selected = weights.Roll(BaseDefinitions(), _equipment.Evaluator, new FixedRandom(0, 0));
        Assert.Equal(EquipmentType.TwoHanded, _equipment.Evaluator.GetArchetype(selected.ArchetypeId).EquipmentType);
        var onlyOffHands = BaseDefinitions().Where(definition =>
            _equipment.Evaluator.GetArchetype(definition.ArchetypeId).EquipmentType == EquipmentType.OffHand).ToArray();
        Assert.Throws<InvalidOperationException>(() => weights.Roll(onlyOffHands, _equipment.Evaluator, new Random(1)));
        Assert.Throws<InvalidOperationException>(() => Weights.Roll([], _equipment.Evaluator, new Random(1)));
    }

    [Theory]
    [InlineData(-0.1, 0.85, 0.25, 0.6, 0.4)]
    [InlineData(double.NaN, 0.35, 0.25, 0.6, 0.4)]
    [InlineData(double.PositiveInfinity, 0.35, 0.25, 0.6, 0.4)]
    [InlineData(0.4, 0.35, 0.2, 0.6, 0.4)]
    [InlineData(0, 0, 0, 0.6, 0.4)]
    [InlineData(0.4, 0.35, 0.25, -0.1, 1.1)]
    [InlineData(0.4, 0.35, 0.25, 0.6, double.NaN)]
    [InlineData(0.4, 0.35, 0.25, 0.6, double.PositiveInfinity)]
    [InlineData(0.4, 0.35, 0.25, 0.5, 0.4)]
    [InlineData(0.4, 0.35, 0.25, 0, 0)]
    public void Invalid_distributions_are_rejected(double weapons, double armor, double jewelry, double oneHanded, double twoHanded)
    {
        var weights = new EquipmentSelectionWeights(weapons, armor, jewelry, oneHanded, twoHanded);
        Assert.Throws<ArgumentException>(weights.Validate);
    }

    private EquipmentDefinition[] BaseDefinitions() => _equipment.Evaluator.Definitions
        .Where(definition => definition.Rarity == EquipmentRarity.Common && definition.NativeStyleId is null).ToArray();

    private static string Group(EquipmentType type) => type switch
    {
        EquipmentType.OneHanded or EquipmentType.OffHand => "one-handed",
        EquipmentType.TwoHanded => "two-handed",
        EquipmentType.Head or EquipmentType.Chest or EquipmentType.Legs => "armor",
        EquipmentType.Ring or EquipmentType.Necklace or EquipmentType.Relic => "jewelry",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private sealed class FixedRandom(double categoryRoll, double handednessRoll = 0, int itemIndex = 0, int? expectedPoolSize = null) : Random
    {
        private readonly Queue<double> _rolls = new([categoryRoll, handednessRoll]);
        public override double NextDouble() => _rolls.Dequeue();
        public override int Next(int maxValue)
        {
            if (expectedPoolSize.HasValue) Assert.Equal(expectedPoolSize.Value, maxValue);
            Assert.InRange(itemIndex, 0, maxValue - 1);
            return itemIndex;
        }
    }
}
