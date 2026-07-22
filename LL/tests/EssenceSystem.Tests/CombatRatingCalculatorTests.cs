using Domain.Components.Attributes;
using Domain.Models.Attributes;

namespace EssenceSystem.Tests;

public sealed class CombatRatingCalculatorTests
{
    [Fact]
    public void Calculate_values_max_health_at_one_point_eight_rating_per_point()
    {
        var attributes = new Dictionary<AttributeType, float>
        {
            [AttributeType.MaxHealth] = 100
        };

        var rating = CombatRatingCalculator.Calculate(attributes);

        Assert.Equal(180, rating);
    }
}
