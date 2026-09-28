using Domain.Models.Guilds.Missions;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Guilds;

namespace EssenceSystem.Tests;

public sealed class GuildMissionOptionRepositoryTests
{
    [Theory]
    [InlineData(QueryTrackingBehavior.TrackAll)]
    [InlineData(QueryTrackingBehavior.NoTracking)]
    public async Task Weekly_options_include_pending_changes_only_for_the_requested_guild_and_week(
        QueryTrackingBehavior trackingBehavior)
    {
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseQueryTrackingBehavior(trackingBehavior)
            .Options);
        var guildId = Guid.NewGuid();
        var otherGuildId = Guid.NewGuid();
        const string week = "20260622";
        const string previousWeek = "20260615";
        var retained = CreateOption(guildId, week);
        var deleted = CreateOption(guildId, week);
        db.GuildMissionOptions.AddRange(
            retained,
            deleted,
            CreateOption(otherGuildId, week),
            CreateOption(guildId, previousWeek));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        db.GuildMissionOptions.Remove(deleted);
        var replacement = CreateOption(guildId, week);
        replacement.MissionDefinitionId = deleted.MissionDefinitionId;
        db.GuildMissionOptions.AddRange(
            replacement,
            CreateOption(otherGuildId, week),
            CreateOption(guildId, previousWeek));
        var repository = new GuildRepository(db);

        var options = await repository.GetWeeklyMissionOptionsAsync(guildId, week, CancellationToken.None);
        var repeated = await repository.GetWeeklyMissionOptionsAsync(guildId, week, CancellationToken.None);

        var expectedIds = new[] { retained.Id, replacement.Id }.Order().ToList();
        Assert.Equal(expectedIds, options.Select(x => x.Id).Order());
        Assert.Equal(expectedIds, repeated.Select(x => x.Id).Order());
        Assert.Same(replacement, Assert.Single(options, x => x.Id == replacement.Id));
        Assert.Equal(EntityState.Added, db.Entry(replacement).State);
        Assert.Equal(EntityState.Deleted, db.Entry(deleted).State);
        Assert.Equal(EntityState.Unchanged, db.Entry(Assert.Single(options, x => x.Id == retained.Id)).State);
    }

    private static GuildMissionOption CreateOption(Guid guildId, string weekKey) => new()
    {
        GuildId = guildId,
        MissionDefinitionId = Guid.NewGuid(),
        WeekKey = weekKey,
        GeneratedAt = new DateTimeOffset(2026, 6, 22, 0, 0, 0, TimeSpan.Zero),
        ExpiresAt = new DateTimeOffset(2026, 6, 29, 0, 0, 0, TimeSpan.Zero)
    };
}
