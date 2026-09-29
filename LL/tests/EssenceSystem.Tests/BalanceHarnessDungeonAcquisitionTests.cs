using BalanceHarness;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessDungeonAcquisitionTests
{
    private sealed class StudyFactAttribute : FactAttribute
    {
        public StudyFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_DUNGEON_ACQUISITION")))
                Skip = "Requires the bounded acquisition owner and frozen request; ordinary tests run zero fights.";
        }
    }

    [Fact]
    public void Route_policy_uses_visible_forecasts_and_rest_without_treasury_or_outcome_peeking()
    {
        DungeonRouteOption Route(int room, RoomType type, int max, int min = 0) => new()
            { RoomIndex = room, RoomType = type, VigorCostMax = max, VigorCostMin = min };
        var rest = Route(9, RoomType.RestSite, 15);
        Assert.Same(rest, DungeonAcquisitionRunner.ChooseRoute([Route(1, RoomType.Boss, 0), rest]));
        Assert.Equal(3, DungeonAcquisitionRunner.ChooseRoute([Route(0, RoomType.Treasury, 0),
            Route(1, RoomType.Combat, 15), Route(2, RoomType.Combat, 10, 5),
            Route(4, RoomType.Combat, 10, 4), Route(3, RoomType.Combat, 10, 4)]).RoomIndex);
        Assert.Throws<InvalidOperationException>(() => DungeonAcquisitionRunner.ChooseRoute([Route(0, RoomType.Treasury, 0)]));
    }

    [StudyFact]
    public async Task Frozen_full_dungeon_acquisition_qualification()
    {
        var request = HarnessJson.Read<DungeonAcquisitionRequest>(Environment.GetEnvironmentVariable("LL_DUNGEON_ACQUISITION")!);
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(14));
        await DungeonAcquisitionStudy.RunAsync(request, deadline.Token);
    }
}
