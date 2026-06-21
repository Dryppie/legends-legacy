using Domain.Models.Essences;
using Services.LL.Essences;

namespace EssenceSystem.Tests;

public sealed class EssenceProgressionServiceTests
{
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

        var result = service.GrantXp(essence, EssenceDefinitionValidatorTests.ValidDefinition(), 10_000);

        Assert.Equal(10, essence.Level);
        Assert.Equal(0, essence.CurrentXp);
        Assert.True(result.ReachedTierCap);
        Assert.Equal(1, result.LevelsGained);
        Assert.True(result.XpGained < 10_000);
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
    public void Ability_value_scaling_uses_level_and_effect_specific_ascension_growth()
    {
        var scaled = EssenceProgressionConstants.ScaleAbilityValue(
            baseValue: 100,
            level: 3,
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
