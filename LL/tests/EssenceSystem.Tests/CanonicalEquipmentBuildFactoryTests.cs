using Domain.Models.Attributes;
using Domain.Helpers;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class CanonicalEquipmentBuildFactoryTests
{
    private readonly CanonicalEquipmentBuildFactory _factory =
        new(Options.Create(new CraftingBalanceOptions()));

    [Fact]
    public void Ladder_is_deterministic_ordered_and_reaches_maximum_progression()
    {
        var first = _factory.GetProgressionLadder();
        var second = _factory.GetProgressionLadder();

        Assert.Equal(first, second);
        Assert.Equal(57, first.Count);
        Assert.Equal(1, first[0].Tier);
        Assert.Equal(0, first[0].EquippedSlotCount);
        Assert.Equal("t1-base", first[0].Id);
        Assert.Equal(10, first[^1].Tier);
        Assert.Equal("t10-masterwork-legacy", first[^1].Id);

        Assert.Equal(first.Count, first.Select(rung => rung.Id).Distinct().Count());
    }

    [Fact]
    public void Entry_progression_acquires_whole_real_slots_before_a_full_loadout()
    {
        var entry = _factory.GetProgressionLadder().Take(7).ToList();

        Assert.Equal(Enumerable.Range(0, 7), entry.Select(rung => rung.EquippedSlotCount));
        Assert.Equal(0d, _factory.CreateBuild(CanonicalPartyProfile.Balanced, entry[0]).AuthorizedBudget);
        Assert.Null(_factory.CreateBuild(CanonicalPartyProfile.Balanced, entry[1]).MainHandRecipeId);
        Assert.NotNull(_factory.CreateBuild(CanonicalPartyProfile.Balanced, entry[2]).MainHandRecipeId);
    }

    [Fact]
    public void Base_rung_uses_real_character_attributes()
    {
        var rung = _factory.GetProgressionLadder().Single(candidate => candidate.Id == "t1-base");
        var build = _factory.CreateBuild(CanonicalPartyProfile.Balanced, rung);
        var expected = EntityBaseAttributeHelper.CreateEntityAttributes(Guid.Empty)
            .ToDictionary(attribute => attribute.AttributeType, attribute => attribute.Value);
        var actual = build.Character.BaseAttributes
            .ToDictionary(attribute => attribute.AttributeType, attribute => attribute.Value);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Equal_rungs_authorize_the_same_budget_for_every_profile()
    {
        var rung = _factory.GetProgressionLadder()[27];
        var builds = Enum.GetValues<CanonicalPartyProfile>()
            .Select(profile => _factory.CreateBuild(profile, rung))
            .ToList();

        Assert.Single(builds.Select(build => build.AuthorizedBudget).Distinct());
        Assert.All(builds, build =>
        {
            Assert.Equal(EquipmentStatBudgetCatalog.BalanceVersion, build.EquipmentBalanceVersion);
            Assert.InRange(
                build.SpentBudget,
                0.99d * build.AuthorizedBudget,
                build.AuthorizedBudget + 0.000001d);
        });
    }

    [Fact]
    public void Combat_rating_uses_canonical_direct_attributes_and_rises_with_progression()
    {
        var ratings = _factory.GetProgressionLadder()
            .Select(rung =>
            {
                var build = _factory.CreateBuild(CanonicalPartyProfile.Balanced, rung);
                return (rung.Id, build.Rating.Overall, build.AuthorizedBudget, build.SpentBudget);
            })
            .ToList();

        Assert.All(ratings, item => Assert.True(item.Overall > 0));
        Assert.All(
            ratings.Zip(ratings.Skip(1)),
            pair => Assert.True(
                pair.Second.Overall > pair.First.Overall,
                $"Canonical Combat Rating did not increase: " +
                $"{pair.First.Id} {pair.First.Overall} ({pair.First.SpentBudget:0.##}/" +
                $"{pair.First.AuthorizedBudget:0.##}) -> " +
                $"{pair.Second.Id} {pair.Second.Overall} ({pair.Second.SpentBudget:0.##}/" +
                $"{pair.Second.AuthorizedBudget:0.##})."));
    }

    [Fact]
    public void Builds_are_deterministic_and_respect_combined_combat_caps()
    {
        var rung = _factory.GetProgressionLadder()[^1];
        var first = _factory.CreateBuild(CanonicalPartyProfile.Sustain, rung);
        var second = _factory.CreateBuild(CanonicalPartyProfile.Sustain, rung);

        Assert.Equal(first.EquipmentPoints, second.EquipmentPoints);
        Assert.Equal(
            first.Character.BaseAttributes.Select(attribute => (attribute.AttributeType, attribute.Value)),
            second.Character.BaseAttributes.Select(attribute => (attribute.AttributeType, attribute.Value)));

        var attributes = first.Character.BaseAttributes.ToDictionary(
            attribute => attribute.AttributeType,
            attribute => attribute.Value);
        foreach (var attribute in EquipmentStatBudgetCatalog.Attributes)
        {
            if (AttributeCatalog.TryGetEffectiveCharacterCap(
                    attribute,
                    EquipmentConstraintProfile.MinimumSupportedBasicAttackIntervalMultiplier,
                    out var cap))
            {
                Assert.True(
                    attributes.GetValueOrDefault(attribute) <= cap + 0.001f,
                    $"{attribute} exceeded its effective combat cap.");
            }
        }
    }
}
