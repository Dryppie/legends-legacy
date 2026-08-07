using Domain.Models.Essences;
using Domain.Models.Snapshots;
using Services.LL.Essences;

namespace EssenceSystem.Tests;

public sealed class EssenceProgressionServiceTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 30)]
    [InlineData(2, 60)]
    [InlineData(3, 100)]
    public void Ascension_tier_determines_level_cap(int ascensionTier, int expectedLevelCap)
    {
        Assert.Equal(expectedLevelCap, EssenceProgressionConstants.GetLevelCap(ascensionTier));
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 30)]
    [InlineData(3, 60)]
    public void Ascension_requires_reaching_the_current_tier_cap(int nextAscensionTier, int requiredLevel)
    {
        var requirement = EssenceProgressionConstants.GetAscensionRequirement(nextAscensionTier);

        Assert.Equal(requiredLevel, requirement.RequiredLevel);
    }

    [Fact]
    public void GrantXp_does_not_bank_xp_past_current_tier_cap()
    {
        var service = new EssenceProgressionService();
        var essence = new PlayerEssence
        {
            EssenceDefinitionId = "essence.test",
            Level = 9,
            CurrentXp = 0,
            AscensionTier = 0
        };

        var result = service.GrantXp(essence, EssenceDefinitionValidatorTests.ValidDefinition(), int.MaxValue);

        Assert.Equal(10, essence.Level);
        Assert.Equal(0, essence.CurrentXp);
        Assert.True(result.ReachedTierCap);
        Assert.Equal(1, result.LevelsGained);
        Assert.True(result.XpGained < int.MaxValue);
    }

    [Fact]
    public void GrantXp_returns_no_gain_when_already_at_current_tier_cap()
    {
        var service = new EssenceProgressionService();
        var essence = new PlayerEssence
        {
            EssenceDefinitionId = "essence.test",
            Level = 10,
            CurrentXp = 0,
            AscensionTier = 0
        };

        var result = service.GrantXp(essence, EssenceDefinitionValidatorTests.ValidDefinition(), 500);

        Assert.Equal(10, essence.Level);
        Assert.Equal(0, essence.CurrentXp);
        Assert.Equal(0, result.XpGained);
        Assert.True(result.ReachedTierCap);
    }

    [Fact]
    public void GrantXp_uses_ascension_tier_for_level_cap()
    {
        var service = new EssenceProgressionService();
        var essence = new PlayerEssence
        {
            EssenceDefinitionId = "essence.test",
            Level = 10,
            CurrentXp = 0,
            AscensionTier = 1
        };

        var result = service.GrantXp(essence, EssenceDefinitionValidatorTests.ValidDefinition(), int.MaxValue);

        Assert.Equal(30, essence.Level);
        Assert.Equal(0, essence.CurrentXp);
        Assert.True(result.ReachedTierCap);
        Assert.Equal(20, result.LevelsGained);
    }

    [Fact]
    public void GrantXp_allows_levels_up_to_100_after_final_ascension()
    {
        var service = new EssenceProgressionService();
        var essence = new PlayerEssence
        {
            EssenceDefinitionId = "essence.test",
            Level = 99,
            CurrentXp = 0,
            AscensionTier = 3
        };

        var result = service.GrantXp(essence, EssenceDefinitionValidatorTests.ValidDefinition(), int.MaxValue);

        Assert.Equal(100, essence.Level);
        Assert.Equal(1, result.LevelsGained);
        Assert.True(result.ReachedTierCap);
    }

    [Fact]
    public void First_ten_levels_require_about_five_days_of_area_one_experience()
    {
        const int areaOneExperiencePerHour = 10_800;
        var required = Enumerable.Range(1, 9)
            .Sum(EssenceProgressionConstants.GetXpRequiredForLevel);

        Assert.InRange(required, areaOneExperiencePerHour * 24 * 5 - 10, areaOneExperiencePerHour * 24 * 5 + 10);
    }

    [Fact]
    public void EquippedEssenceSnapshot_round_trips_level_and_ascension_tier()
    {
        var essence = new PlayerEssence
        {
            Id = Guid.NewGuid(),
            CharacterId = Guid.NewGuid(),
            EssenceDefinitionId = "essence.test",
            Level = 31,
            CurrentXp = 25,
            AscensionTier = 2,
            IsEvolved = true
        };

        var snapshot = EquippedEssenceSnapshot.From(Guid.NewGuid(), 1, essence);
        var restored = snapshot.ToPlayerEssence(essence.CharacterId);

        Assert.Equal(31, snapshot.Level);
        Assert.Equal(2, snapshot.AscensionTier);
        Assert.Equal(31, restored.Level);
        Assert.Equal(2, restored.AscensionTier);
    }

    [Fact]
    public void Ability_value_scaling_uses_effect_specific_ascension_growth()
    {
        var scaled = EssenceProgressionConstants.ScaleAbilityValue(
            baseValue: 100,
            ascensionTier: 1,
            effectType: "Damage");

        Assert.Equal(112d, scaled, 6);
    }

    [Fact]
    public void Active_cooldown_scaling_reduces_cooldown_with_cap()
    {
        var scaled = EssenceProgressionConstants.ScaleActiveCooldownSeconds(baseCooldownSeconds: 20, ascensionTier: 3);

        Assert.Equal(17d, scaled);
    }

    [Fact]
    public void Duration_scaling_increases_soft_statuses_but_not_hard_crowd_control()
    {
        var poison = EssenceProgressionConstants.ScaleEffectDurationSeconds(
            baseDurationSeconds: 10,
            ascensionTier: 2,
            effectType: "ApplyStatus",
            statusId: "Poison");
        var stun = EssenceProgressionConstants.ScaleEffectDurationSeconds(
            baseDurationSeconds: 10,
            ascensionTier: 2,
            effectType: "ApplyStatus",
            statusId: "Stunned");

        Assert.Equal(11d, poison);
        Assert.Equal(10d, stun);
    }
}
