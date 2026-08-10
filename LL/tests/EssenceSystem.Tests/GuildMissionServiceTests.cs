using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services.LL.Guilds;
using Domain.Models.Entities.Characters;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Missions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence.LL;
using Services.LL.Guilds;

namespace EssenceSystem.Tests;

public sealed class GuildMissionServiceTests
{
    [Fact]
    public void Weekly_targets_match_five_day_activity_benchmarks()
    {
        var missions = new DefaultGuildContentProvider().WeeklyMissions.ToDictionary(x => x.Key);

        Assert.Equal(432_000, missions["weekly.monster_extermination"].BaseTarget);
        Assert.Equal(1_000, missions["weekly.dungeon_expedition"].BaseTarget);
        Assert.Equal(432_000, missions["weekly.craftsmens_commission"].BaseTarget);
        Assert.Equal(100, missions["weekly.dungeon_vanguard"].BaseTarget);
        Assert.DoesNotContain(missions.Values, mission => mission.Metric == GuildContributionMetric.ItemsCrafted);

        const long fiveDaysOfTenSecondActions = 5 * 24 * 60 * 60 / 10;
        Assert.Equal(fiveDaysOfTenSecondActions * 10, missions["weekly.monster_extermination"].BaseTarget);
        Assert.Equal(fiveDaysOfTenSecondActions, missions["weekly.craftsmens_commission"].BaseTarget * 10 / 100);
        Assert.Equal(20 * 5 * 10, missions["weekly.dungeon_expedition"].BaseTarget);
        Assert.Equal(2 * 5 * 10, missions["weekly.dungeon_vanguard"].BaseTarget);
    }

    [Fact]
    public void Json_weekly_targets_match_code_fallback()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var json = new JsonGuildContentProvider(
            new ConfigurationBuilder().Build(),
            AppContext.BaseDirectory,
            options);
        var fallback = new DefaultGuildContentProvider();

        Assert.Equal(fallback.WeeklyMissions, json.WeeklyMissions);
    }

    [Fact]
    public async Task Platinum_requires_five_days_of_maximum_tempering_actions_even_after_guild_completion()
    {
        await using var db = CreateDbContext();
        var characterId = SeedGuild(db);
        var guild = db.Guilds.Local.Single();
        var now = new DateTimeOffset(2026, 6, 23, 2, 0, 0, TimeSpan.Zero);
        var definition = new DefaultGuildContentProvider().WeeklyMissions
            .Single(x => x.Key == "weekly.craftsmens_commission");
        var contribution = new GuildMissionContribution
        {
            GuildId = guild.Id,
            CharacterId = characterId,
            Amount = 43_199
        };
        db.GuildMissionInstances.Add(new GuildMissionInstance
        {
            GuildId = guild.Id,
            MissionDefinitionId = definition.Id,
            WeekKey = "20260622",
            TargetAmount = definition.BaseTarget,
            CurrentAmount = definition.BaseTarget,
            Status = GuildMissionStatus.Completed,
            StartedAt = now.AddDays(-1),
            EndsAt = new DateTimeOffset(2026, 6, 29, 0, 0, 0, TimeSpan.Zero),
            RewardClaimDeadline = new DateTimeOffset(2026, 7, 6, 0, 0, 0, TimeSpan.Zero),
            Contributions = { contribution }
        });
        await db.SaveChangesAsync();
        var service = new GuildMissionService(db);

        var beforePlatinum = await service.GetOverviewAsync(characterId, now, CancellationToken.None);
        var finalAction = await service.RecordContributionAsync(
            new GuildContributionEvent(
                characterId,
                GuildContributionSource.Tempering,
                GuildContributionMetric.TemperingActionsCompleted,
                1,
                OccurredAt: now,
                IdempotencyKey: "tempering:platinum-action"),
            CancellationToken.None);
        var atPlatinum = await service.GetOverviewAsync(characterId, now, CancellationToken.None);

        Assert.Equal(GuildContributionTier.Gold, beforePlatinum!.MyWeeklyContribution!.Tier);
        Assert.Equal(
            [
                GuildContributionTier.Bronze,
                GuildContributionTier.Silver,
                GuildContributionTier.Gold,
                GuildContributionTier.Platinum
            ],
            beforePlatinum.ActiveMission!.RewardTiers.Select(x => x.Tier));
        Assert.Equal(
            [10_800L, 21_600L, 32_400L, 43_200L],
            beforePlatinum.ActiveMission.RewardTiers.Select(x => x.RequiredContribution));
        var platinumReward = beforePlatinum.ActiveMission.RewardTiers[^1].Reward;
        Assert.Equal(225, platinumReward.GuildFavor);
        Assert.Equal(650, platinumReward.GuildXp);
        Assert.Equal(130, platinumReward.GuildSupplies);
        Assert.Equal(0, finalAction.WeeklyProgressAdded);
        Assert.Equal(GuildContributionTier.Platinum, atPlatinum!.MyWeeklyContribution!.Tier);
    }

    [Fact]
    public async Task Active_weekly_mission_adopts_the_rebalanced_target()
    {
        await using var db = CreateDbContext();
        var characterId = SeedGuild(db);
        var guild = db.Guilds.Local.Single();
        var now = new DateTimeOffset(2026, 6, 23, 2, 0, 0, TimeSpan.Zero);
        var definition = new DefaultGuildContentProvider().WeeklyMissions
            .Single(x => x.Key == "weekly.craftsmens_commission");
        db.GuildMissionInstances.Add(new GuildMissionInstance
        {
            GuildId = guild.Id,
            MissionDefinitionId = definition.Id,
            WeekKey = "20260622",
            TargetAmount = 250,
            CurrentAmount = 100,
            Status = GuildMissionStatus.Active,
            StartedAt = now.AddDays(-1),
            EndsAt = new DateTimeOffset(2026, 6, 29, 0, 0, 0, TimeSpan.Zero),
            RewardClaimDeadline = new DateTimeOffset(2026, 7, 6, 0, 0, 0, TimeSpan.Zero)
        });
        await db.SaveChangesAsync();
        var service = new GuildMissionService(db);

        var overview = await service.GetOverviewAsync(characterId, now, CancellationToken.None);

        Assert.Equal(432_000, overview!.ActiveMission!.TargetAmount);
    }

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
        Assert.All(overview.PersonalOrders, order =>
        {
            Assert.Equal(50, order.Reward.GuildFavor);
            Assert.Equal(20, order.Reward.GuildXp);
            Assert.Equal(10, order.Reward.GuildSupplies);
        });
        Assert.Null(overview.ActiveMission);
        Assert.True(overview.CanSelectMission);
    }

    [Fact]
    public async Task New_guild_created_midweek_waits_for_an_officer_to_select_a_mission()
    {
        await using var db = CreateDbContext();
        var characterId = SeedGuild(db);
        var now = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero);
        db.Guilds.Local.Single().CreatedAt = now;
        await db.SaveChangesAsync();
        var service = new GuildMissionService(db);

        var overview = await service.GetOverviewAsync(characterId, now, CancellationToken.None);

        Assert.Null(overview!.ActiveMission);
        Assert.True(overview.CanSelectMission);
        Assert.All(overview.WeeklyOptions, option => Assert.False(option.IsSelected));
    }

    [Fact]
    public async Task Removed_weekly_mission_is_cleared_and_replaced_with_valid_choices()
    {
        await using var db = CreateDbContext();
        var characterId = SeedGuild(db);
        var guild = db.Guilds.Local.Single();
        var now = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero);
        var removedMissionId = Guid.Parse("9b2b2643-2bd2-43ac-bb50-5a6d7b6e45c1");
        db.GuildMissionOptions.Add(new GuildMissionOption
        {
            GuildId = guild.Id,
            MissionDefinitionId = removedMissionId,
            WeekKey = "20260622",
            GeneratedAt = now.AddDays(-1),
            ExpiresAt = new DateTimeOffset(2026, 6, 29, 0, 0, 0, TimeSpan.Zero),
            IsSelected = true,
            SelectedAt = now.AddDays(-1),
            SelectedByCharacterId = characterId
        });
        db.GuildMissionInstances.Add(new GuildMissionInstance
        {
            GuildId = guild.Id,
            MissionDefinitionId = removedMissionId,
            WeekKey = "20260622",
            TargetAmount = 16_200,
            CurrentAmount = 100,
            Status = GuildMissionStatus.Active,
            StartedAt = now.AddDays(-1),
            EndsAt = new DateTimeOffset(2026, 6, 29, 0, 0, 0, TimeSpan.Zero),
            RewardClaimDeadline = new DateTimeOffset(2026, 7, 6, 0, 0, 0, TimeSpan.Zero)
        });
        await db.SaveChangesAsync();
        var service = new GuildMissionService(db);

        var overview = await service.GetOverviewAsync(characterId, now, CancellationToken.None);

        Assert.Null(overview!.ActiveMission);
        Assert.Equal(3, overview.WeeklyOptions.Count);
        Assert.DoesNotContain(overview.WeeklyOptions, option => option.Definition.Metric == GuildContributionMetric.ItemsCrafted);
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

        var initial = await service.GetOverviewAsync(characterId, now, CancellationToken.None);
        var monsterMission = initial!.WeeklyOptions.Single(x => x.Definition.Key == "weekly.monster_extermination");
        var selected = await service.SelectMissionAsync(characterId, monsterMission.Id, now, CancellationToken.None);
        Assert.True(selected.Succeeded);
        Assert.Equal("weekly.monster_extermination", selected.Value!.ActiveMission!.Definition.Key);
        Assert.False(selected.Value.CanSelectMission);
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
