using Application.Interfaces.Services.LL.Guilds;
using Domain.Models.Entities.Characters;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Missions;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Services.LL.Guilds;

namespace EssenceSystem.Tests;

public sealed class GuildMissionServiceTests
{
    [Fact]
    public async Task GetOverview_generates_weekly_options_and_daily_orders()
    {
        await using var db = CreateDbContext();
        var characterId = SeedGuild(db);
        await db.SaveChangesAsync();
        var service = new GuildMissionService(db);

        var overview = await service.GetOverviewAsync(characterId, new DateTimeOffset(2026, 6, 22, 1, 0, 0, TimeSpan.Zero), CancellationToken.None);

        Assert.NotNull(overview);
        Assert.Equal(3, overview!.WeeklyOptions.Count);
        Assert.Equal(3, overview.PersonalOrders.Count);
        Assert.Null(overview.ActiveMission);
        Assert.True(overview.CanSelectMission);
    }

    [Fact]
    public async Task RecordContribution_uses_idempotency_key()
    {
        await using var db = CreateDbContext();
        var characterId = SeedGuild(db);
        await db.SaveChangesAsync();
        var service = new GuildMissionService(db);
        var now = new DateTimeOffset(2026, 6, 23, 2, 0, 0, TimeSpan.Zero);

        await service.GetOverviewAsync(characterId, now, CancellationToken.None);
        var first = await service.RecordContributionAsync(
            new GuildContributionEvent(
                characterId,
                GuildContributionSource.Combat,
                GuildContributionMetric.CreaturesDefeated,
                10,
                OccurredAt: now,
                IdempotencyKey: "combat:tick:1"),
            CancellationToken.None);
        await db.SaveChangesAsync();
        var duplicate = await service.RecordContributionAsync(
            new GuildContributionEvent(
                characterId,
                GuildContributionSource.Combat,
                GuildContributionMetric.CreaturesDefeated,
                10,
                OccurredAt: now,
                IdempotencyKey: "combat:tick:1"),
            CancellationToken.None);
        await db.SaveChangesAsync();

        var overview = await service.GetOverviewAsync(characterId, now, CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.True(duplicate.WasDuplicate);
        Assert.Equal(10, overview!.ActiveMission!.CurrentAmount);
        Assert.Equal(10, overview.PersonalOrders.Single(x => x.definitionKey() == "daily.creatures_defeated").CurrentAmount);
    }

    [Fact]
    public async Task RecordContribution_uses_unsaved_idempotency_key()
    {
        await using var db = CreateDbContext();
        var characterId = SeedGuild(db);
        await db.SaveChangesAsync();
        var service = new GuildMissionService(db);
        var now = new DateTimeOffset(2026, 6, 23, 2, 0, 0, TimeSpan.Zero);

        await service.GetOverviewAsync(characterId, now, CancellationToken.None);
        var first = await service.RecordContributionAsync(
            new GuildContributionEvent(
                characterId,
                GuildContributionSource.Dungeon,
                GuildContributionMetric.DungeonRoomsCleared,
                1,
                OccurredAt: now,
                IdempotencyKey: "dungeon-room-cleared:run-1:0"),
            CancellationToken.None);
        var duplicate = await service.RecordContributionAsync(
            new GuildContributionEvent(
                characterId,
                GuildContributionSource.Dungeon,
                GuildContributionMetric.DungeonRoomsCleared,
                1,
                OccurredAt: now,
                IdempotencyKey: "dungeon-room-cleared:run-1:0"),
            CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.True(duplicate.WasDuplicate);
        Assert.Single(db.GuildContributionLedgers.Local, x => x.IdempotencyKey == "dungeon-room-cleared:run-1:0");
    }

    [Fact]
    public async Task ClaimPersonalOrderReward_awards_once()
    {
        await using var db = CreateDbContext();
        var characterId = SeedGuild(db);
        await db.SaveChangesAsync();
        var service = new GuildMissionService(db);
        var now = new DateTimeOffset(2026, 6, 23, 2, 0, 0, TimeSpan.Zero);

        await service.GetOverviewAsync(characterId, now, CancellationToken.None);
        await service.RecordContributionAsync(
            new GuildContributionEvent(
                characterId,
                GuildContributionSource.Combat,
                GuildContributionMetric.CreaturesDefeated,
                100,
                OccurredAt: now,
                IdempotencyKey: "combat:tick:claim"),
            CancellationToken.None);
        await db.SaveChangesAsync();
        var overview = await service.GetOverviewAsync(characterId, now, CancellationToken.None);
        var order = overview!.PersonalOrders.Single(x => x.definitionKey() == "daily.creatures_defeated");

        var claimed = await service.ClaimPersonalOrderRewardAsync(characterId, order.Id, now, CancellationToken.None);
        await db.SaveChangesAsync();
        var claimedAgain = await service.ClaimPersonalOrderRewardAsync(characterId, order.Id, now, CancellationToken.None);
        await db.SaveChangesAsync();

        var character = await db.Characters.SingleAsync(x => x.Id == characterId);
        var guild = await db.Guilds.Include(x => x.Resources).SingleAsync();

        Assert.True(claimed.Succeeded);
        Assert.False(claimedAgain.Succeeded);
        Assert.Equal(50, character.GuildFavor);
        Assert.Equal(10, guild.Resources.Single(x => x.Resource == GuildResourceType.GuildSupplies).Amount);
    }

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static Guid SeedGuild(LLDbContext db)
    {
        var characterId = Guid.NewGuid();
        db.Characters.Add(new Character
        {
            Id = characterId,
            UserId = Guid.NewGuid(),
            Name = "Guild Hero",
            ImagePath = "player",
            Level = 10
        });
        db.Guilds.Add(new Guild
        {
            Id = Guid.NewGuid(),
            Name = "Test Guild",
            OwnerId = characterId,
            Members =
            {
                new GuildMember
                {
                    CharacterId = characterId,
                    Role = GuildRole.Leader,
                    JoinedAt = new DateTimeOffset(2026, 6, 20, 0, 0, 0, TimeSpan.Zero)
                }
            }
        });

        return characterId;
    }
}

internal static class GuildMissionTestExtensions
{
    public static string definitionKey(this PersonalGuildOrderDto order) => order.Definition.Key;
}
