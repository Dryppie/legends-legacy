using Domain.Models.Dungeons.Runs;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;

namespace EssenceSystem.Tests;

public sealed class TelemetryHistoryTests
{
    [Fact]
    public async Task Dungeon_outcome_survives_reward_cleanup()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new LLDbContext(options);
        var started = DateTimeOffset.UtcNow.AddMinutes(-3);
        var run = new DungeonRun
        {
            Id = Guid.NewGuid(), CharacterId = Guid.NewGuid(),
            DungeonDefinitionId = "test-dungeon", CreatedAt = started
        };

        db.DungeonRuns.Add(run);
        await db.SaveChangesAsync();
        run.Status = DungeonRunStatus.Completed;
        run.CompletedAt = started.AddMinutes(2);
        await db.SaveChangesAsync();
        db.DungeonRuns.Remove(run);
        await db.SaveChangesAsync();

        Assert.Empty(await db.DungeonRuns.ToListAsync());
        var history = await db.DungeonAttemptHistories.SingleAsync();
        Assert.Equal(run.Id, history.RunId);
        Assert.Equal("Completed", history.Outcome);
        Assert.Equal(run.CompletedAt, history.FinishedAtUtc);
    }

    [Fact]
    public async Task Terminal_run_removed_in_same_unit_of_work_still_has_history()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new LLDbContext(options);
        var run = new DungeonRun
        {
            Id = Guid.NewGuid(), CharacterId = Guid.NewGuid(),
            DungeonDefinitionId = "test-dungeon", CreatedAt = DateTimeOffset.UtcNow
        };
        db.DungeonRuns.Add(run);
        await db.SaveChangesAsync();

        run.Status = DungeonRunStatus.Failed;
        db.DungeonRuns.Remove(run);
        await db.SaveChangesAsync();

        var history = await db.DungeonAttemptHistories.SingleAsync();
        Assert.Equal("Failed", history.Outcome);
        Assert.NotNull(history.FinishedAtUtc);
    }
}
