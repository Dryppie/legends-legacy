using Domain.Models.Prophecies;
using Services.LL.Prophecies;

namespace EssenceSystem.Tests;

public sealed class ProphecyOfferSelectorTests
{
    private static readonly Guid CharacterId = Guid.Parse("7d27d315-9b34-446e-a654-1602a572b513");
    private static readonly DateTimeOffset PeriodStart = new(2026, 7, 14, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Pick_avoids_used_definitions_and_categories_when_possible()
    {
        var definitions = new[]
        {
            Definition("combat.used", ProphecyCategory.Combat, weight: 1000),
            Definition("essence.available", ProphecyCategory.Essence)
        };

        var result = ProphecyOfferSelector.Pick(
            definitions,
            ProphecyScope.Daily,
            ProphecySlotType.Focused,
            CharacterId,
            PeriodStart,
            "initial",
            new HashSet<string>(["combat.used"], StringComparer.OrdinalIgnoreCase),
            new HashSet<ProphecyCategory> { ProphecyCategory.Combat });

        Assert.Equal("essence.available", result?.Id);
    }

    [Fact]
    public void Pick_returns_a_different_definition_for_reroll_when_an_alternative_exists()
    {
        var definitions = new[]
        {
            Definition("current", ProphecyCategory.Combat, weight: 1000),
            Definition("replacement", ProphecyCategory.Combat)
        };

        var result = ProphecyOfferSelector.Pick(
            definitions,
            ProphecyScope.Daily,
            ProphecySlotType.Focused,
            CharacterId,
            PeriodStart,
            "reroll",
            new HashSet<string>(["current"], StringComparer.OrdinalIgnoreCase));

        Assert.Equal("replacement", result?.Id);
    }

    [Fact]
    public void Pick_is_deterministic_for_the_same_period_and_salt()
    {
        var definitions = new[]
        {
            Definition("one", ProphecyCategory.Combat),
            Definition("two", ProphecyCategory.Dungeon),
            Definition("three", ProphecyCategory.Essence)
        };

        var first = ProphecyOfferSelector.Pick(
            definitions,
            ProphecyScope.Daily,
            ProphecySlotType.Focused,
            CharacterId,
            PeriodStart,
            "initial");
        var second = ProphecyOfferSelector.Pick(
            definitions,
            ProphecyScope.Daily,
            ProphecySlotType.Focused,
            CharacterId,
            PeriodStart,
            "initial");

        Assert.Equal(first?.Id, second?.Id);
    }

    [Fact]
    public void Pick_excludes_definitions_above_the_character_level()
    {
        var definitions = new[]
        {
            Definition("level-one", ProphecyCategory.Combat, minPlayerLevel: 1),
            Definition("level-five", ProphecyCategory.Combat, minPlayerLevel: 5)
        };

        var result = ProphecyOfferSelector.Pick(
            definitions,
            ProphecyScope.Daily,
            ProphecySlotType.Focused,
            CharacterId,
            PeriodStart,
            "level-filter",
            characterLevel: 1);

        Assert.Equal("level-one", result?.Id);
    }

    private static ProphecyDefinition Definition(
        string id,
        ProphecyCategory category,
        int weight = 100,
        int minPlayerLevel = 1) =>
        new()
        {
            Id = id,
            Scope = ProphecyScope.Daily,
            Category = category,
            IsEnabled = true,
            Weight = weight,
            MinPlayerLevel = minPlayerLevel,
            AllowedSlots = [ProphecySlotType.Focused.ToString()]
        };
}
