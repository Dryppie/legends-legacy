using Domain.Models.Entities.Creatures;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;

namespace EssenceSystem.Tests;

public sealed class CombatCinderRewardCalculatorTests
{
    private static readonly DefaultIdleCinderRewardCalculator Calculator = new(
        Options.Create(new CombatCinderRewardOptions
        {
            RewardBasisPointsOfCreatureExperience = 2000,
            MinimumCindersPerVictory = 1
        }));

    [Fact]
    public void Calculate_returns_zero_without_positive_creature_experience()
    {
        var reward = Calculator.Calculate(
        [
            new Creature { ExperienceReward = 0 },
            new Creature { ExperienceReward = -10 }
        ]);

        Assert.Equal(0, reward);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(4, 1)]
    [InlineData(10, 2)]
    [InlineData(34, 7)]
    [InlineData(45, 9)]
    public void Calculate_grants_twenty_percent_of_creature_experience_rounded_up(
        int creatureExperience,
        int expectedCinders)
    {
        var reward = Calculator.Calculate(
            [new Creature { ExperienceReward = creatureExperience }]);

        Assert.Equal(expectedCinders, reward);
    }

    [Fact]
    public void Calculate_combines_all_defeated_creatures_before_rounding()
    {
        var reward = Calculator.Calculate(
        [
            new Creature { ExperienceReward = 15 },
            new Creature { ExperienceReward = 20 }
        ]);

        Assert.Equal(7, reward);
    }
}
