using Domain.Models.Regions.Areas;
using Services.LL.Spawnings;

namespace EssenceSystem.Tests;

public sealed class EssenceFocusSpawnTests
{
    [Fact]
    public void Focus_increases_normalized_probability_by_twenty_percent_without_mutating_area()
    {
        AreaCreature[] creatures =
        [
            new() { AreaId = "area", CreatureId = Guid.NewGuid(), WeightedSpawnRate = 2 },
            new() { AreaId = "area", CreatureId = Guid.NewGuid(), WeightedSpawnRate = 3 },
            new() { AreaId = "area", CreatureId = Guid.NewGuid(), WeightedSpawnRate = 5 }
        ];
        var focused = WeightedSpawnSelector.ApplyEssenceFocus(creatures, new HashSet<Guid> { creatures[0].CreatureId });

        Assert.Equal(0.24, focused[0].WeightedSpawnRate / focused.Sum(x => x.WeightedSpawnRate), 6);
        Assert.Equal(3d / 5, focused[1].WeightedSpawnRate / focused[2].WeightedSpawnRate, 6);
        Assert.Equal([2f, 3f, 5f], creatures.Select(x => x.WeightedSpawnRate));
        Assert.Equal(creatures.Select(x => x.CreatureId), focused.Select(x => x.CreatureId));
        Assert.All(focused, creature => Assert.Equal("area", creature.AreaId));
        Assert.NotSame(creatures[0], focused[0]);
    }

    [Theory]
    [InlineData(0f, 1f, 0f)]
    [InlineData(9f, 1f, 1f)]
    [InlineData(1f, 0f, 1f)]
    public void Focus_respects_unavailable_spawns_and_caps_at_certainty(float weight, float other, float expected)
    {
        AreaCreature[] creatures =
        [
            new() { CreatureId = Guid.NewGuid(), WeightedSpawnRate = weight },
            new() { CreatureId = Guid.NewGuid(), WeightedSpawnRate = other }
        ];
        var focused = WeightedSpawnSelector.ApplyEssenceFocus(creatures, new HashSet<Guid> { creatures[0].CreatureId });
        Assert.Equal(expected, focused[0].WeightedSpawnRate / focused.Sum(x => x.WeightedSpawnRate), 6);
    }

    [Fact]
    public void Focus_outside_an_area_leaves_its_spawn_table_unchanged()
    {
        AreaCreature[] creatures = [new() { CreatureId = Guid.NewGuid(), WeightedSpawnRate = 1 }];
        Assert.Same(creatures, WeightedSpawnSelector.ApplyEssenceFocus(creatures, new HashSet<Guid> { Guid.NewGuid() }));
    }
}
