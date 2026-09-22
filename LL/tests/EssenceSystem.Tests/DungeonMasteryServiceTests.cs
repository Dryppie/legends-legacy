using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Mastery;
using Domain.Models.Dungeons.Runs;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Dungeons;
using Services.LL.Dungeons;

namespace EssenceSystem.Tests;

public sealed class DungeonMasteryServiceTests
{
    [Theory]
    [InlineData(1, 1000)]
    [InlineData(2, 2500)]
    [InlineData(3, 5000)]
    [InlineData(4, 9000)]
    [InlineData(5, 14000)]
    [InlineData(6, 21000)]
    [InlineData(7, 30000)]
    [InlineData(8, 42000)]
    [InlineData(9, 56000)]
    [InlineData(10, 75000)]
    public void Curve_requires_ten_times_the_original_cumulative_experience(int level, int threshold)
    {
        Assert.Equal(level - 1, DungeonMasteryProgression.CalculateLevel(threshold - 1));
        Assert.Equal(level, DungeonMasteryProgression.CalculateLevel(threshold));
        Assert.Equal(threshold, DungeonMasteryProgression.GetExperienceRequiredForNextLevel(level - 1));
        Assert.Null(DungeonMasteryProgression.GetExperienceRequiredForNextLevel(10));
        Assert.Equal(10, DungeonMasteryProgression.CalculateLevel(long.MaxValue));
        Assert.Equal(0, DungeonMasteryProgression.CalculateLevel(-1));
    }

    [Fact]
    public async Task Clears_on_all_difficulties_share_one_persisted_mastery_and_retries_cannot_repeat_XP()
    {
        await using var db = CreateDb();
        var service = new DungeonMasteryService(new CharacterDungeonMasteryRepository(db));
        var character = Guid.NewGuid();
        var novice = Run(character, "goblin_mines_i");
        var veteran = Run(character, "goblin_mines_ii");
        var champion = Run(character, "goblin_mines_iii");

        foreach (var run in new[] { novice, veteran, champion })
        {
            var award = await service.AwardRunMasteryAsync(run, default);
            // Three completed rooms + boss + miniboss, without an XP nerf per clear.
            Assert.Equal(190, award.ExperienceAwarded);
            Assert.Equal("goblin_mines", award.DungeonDefinitionId);
        }

        var retry = await service.AwardRunMasteryAsync(novice, default);
        Assert.True(retry.AlreadyAwarded);
        Assert.Equal(0, retry.ExperienceAwarded);

        string[] ids = ["goblin_mines_i", "goblin_mines_ii", "goblin_mines_iii", "tangled_cave_ii"];
        var beforeSave = await service.GetMasteryByDungeonAsync(character, ids, default);
        Assert.Equal(570, beforeSave["goblin_mines_iii"].Experience);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var stored = Assert.Single(await db.CharacterDungeonMasteries.ToListAsync());
        Assert.Equal("goblin_mines", stored.DungeonDefinitionId);
        Assert.Equal(3, stored.CompletionCount);
        Assert.Equal(570, stored.Experience);
        Assert.Equal(0, stored.Level);

        var previews = await service.GetMasteryByDungeonAsync(character, ids, default);
        Assert.All(ids.Take(3), id =>
        {
            Assert.Equal(id, previews[id].DungeonDefinitionId);
            Assert.Equal(570, previews[id].Experience);
            Assert.Equal(3, previews[id].CompletionCount);
            Assert.Equal(1000, previews[id].ExperienceRequiredForNextLevel);
        });
        Assert.Equal(0, previews["tangled_cave_ii"].Experience);
        Assert.All((await service.GetMasteryByDungeonAsync(Guid.NewGuid(), ids, default)).Values,
            mastery => Assert.Equal(0, mastery.Experience));

        await service.AwardRunMasteryAsync(Run(character, "tangled_cave_ii"), default);
        await db.SaveChangesAsync();
        Assert.Equal(2, await db.CharacterDungeonMasteries.CountAsync());
        Assert.Equal(570, (await db.CharacterDungeonMasteries.SingleAsync(x => x.DungeonDefinitionId == "goblin_mines")).Experience);
    }

    [Fact]
    public async Task Failed_and_retreated_runs_award_progress_without_counting_as_completions()
    {
        await using var db = CreateDb();
        var service = new DungeonMasteryService(new CharacterDungeonMasteryRepository(db));
        var character = Guid.NewGuid();
        var failed = new DungeonRun
        {
            Id = Guid.NewGuid(), CharacterId = character, DungeonDefinitionId = "goblin_mines_i",
            Status = DungeonRunStatus.Failed, CurrentRoomIndex = 2,
            State = new DungeonRunState
            {
                FailureAnalysis = new DungeonFailureAnalysis { PrimaryCause = "Combat Readiness" }
            },
            Rooms =
            [
                new() { RoomIndex = 0, Type = RoomType.Entrance, Status = RoomInstanceStatus.Completed },
                new() { RoomIndex = 1, Type = RoomType.Combat, Status = RoomInstanceStatus.Completed },
                // FailRun marks this room completed even though the boss won.
                new() { RoomIndex = 2, Type = RoomType.Boss, Status = RoomInstanceStatus.Completed }
            ]
        };
        var retreated = new DungeonRun
        {
            Id = Guid.NewGuid(), CharacterId = character, DungeonDefinitionId = "goblin_mines_ii",
            Status = DungeonRunStatus.Retreated, CurrentRoomIndex = 3,
            Rooms =
            [
                new() { RoomIndex = 0, Type = RoomType.Entrance, Status = RoomInstanceStatus.Completed },
                new() { RoomIndex = 1, Type = RoomType.Combat, Status = RoomInstanceStatus.Completed },
                new() { RoomIndex = 2, Type = RoomType.MiniBoss, Status = RoomInstanceStatus.Completed },
                new() { RoomIndex = 3, Type = RoomType.Boss, Status = RoomInstanceStatus.Pending }
            ]
        };

        var failedAward = await service.AwardRunMasteryAsync(failed, default);
        var retreatedAward = await service.AwardRunMasteryAsync(retreated, default);

        Assert.Equal(10, failedAward.ExperienceAwarded);
        Assert.Collection(
            failedAward.Reasons,
            reason => Assert.Equal("attempt", reason.Id),
            reason => Assert.Equal("rooms_cleared", reason.Id));
        Assert.Equal(40, retreatedAward.ExperienceAwarded);
        Assert.Contains(retreatedAward.Reasons, reason => reason.Id == "miniboss_defeated");
        var mastery = Assert.Single(db.CharacterDungeonMasteries.Local);
        Assert.Equal(50, mastery.Experience);
        Assert.Equal(0, mastery.CompletionCount);
    }

    [Fact]
    public async Task Reaching_mastery_cap_on_a_failed_run_defers_the_one_time_completion_reward()
    {
        await using var db = CreateDb();
        var character = Guid.NewGuid();
        db.CharacterDungeonMasteries.Add(new CharacterDungeonMastery
        {
            CharacterId = character, DungeonDefinitionId = "goblin_mines", Experience = 74995, Level = 9
        });
        await db.SaveChangesAsync();
        var service = new DungeonMasteryService(new CharacterDungeonMasteryRepository(db));
        var failed = new DungeonRun
        {
            Id = Guid.NewGuid(), CharacterId = character, DungeonDefinitionId = "goblin_mines_i",
            Status = DungeonRunStatus.Failed, State = new DungeonRunState(), Rooms = []
        };

        var failedAward = await service.AwardRunMasteryAsync(failed, default);
        var completionAward = await service.AwardRunMasteryAsync(Run(character, "goblin_mines_ii"), default);

        Assert.Equal(10, failedAward.Level);
        Assert.False(failedAward.UnlocksMaxLevelReward);
        Assert.True(completionAward.UnlocksMaxLevelReward);
        Assert.True(Assert.Single(db.CharacterDungeonMasteries.Local).MaxLevelRewardClaimed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Shared_mastery_cap_reward_is_once_per_family_even_after_progress_recalculation(bool previouslyClaimed)
    {
        await using var db = CreateDb();
        var character = Guid.NewGuid();
        db.CharacterDungeonMasteries.Add(new CharacterDungeonMastery
        {
            CharacterId = character, DungeonDefinitionId = "goblin_mines", Experience = 74900,
            Level = 9, MaxLevelRewardClaimed = previouslyClaimed
        });
        await db.SaveChangesAsync();
        var service = new DungeonMasteryService(new CharacterDungeonMasteryRepository(db));

        var award = await service.AwardRunMasteryAsync(Run(character, "goblin_mines_ii"), default);
        Assert.Equal(10, award.Level);
        Assert.Equal(!previouslyClaimed, award.UnlocksMaxLevelReward);
        Assert.True(Assert.Single(db.CharacterDungeonMasteries.Local).MaxLevelRewardClaimed);
        var next = await service.AwardRunMasteryAsync(Run(character, "goblin_mines_iii"), default);
        Assert.False(next.UnlocksMaxLevelReward);
    }

    private static DungeonRun Run(Guid character, string id) => new()
    {
        Id = Guid.NewGuid(), CharacterId = character, DungeonDefinitionId = id,
        Status = DungeonRunStatus.Completed,
        Rooms =
        [
            new() { Type = RoomType.Combat, Status = RoomInstanceStatus.Completed },
            new() { Type = RoomType.MiniBoss, Status = RoomInstanceStatus.Completed },
            new() { Type = RoomType.Boss, Status = RoomInstanceStatus.Completed },
            new() { Type = RoomType.Combat, Status = RoomInstanceStatus.Pending }
        ]
    };

    private static LLDbContext CreateDb() => new(new DbContextOptionsBuilder<LLDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
