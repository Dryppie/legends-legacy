using Application.UseCases.Outbox;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;
using Domain.Models.Guilds.Missions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;
using Persistence.LL.Repositories.Guilds;
using Services.LL.Guilds;

namespace EssenceSystem.Tests;

public sealed partial class EssenceSystemServiceTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(4, true)]
    public async Task Absorb_or_shatter_completes_new_and_existing_guild_orders(
        int shatterQuantity, bool existingOrder)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var db = new LLDbContext(options);
        var characterId = await SeedCharacterAndInventoryAsync(db);
        var itemId = await AddEssenceItemAsync(db, characterId, quantity: shatterQuantity + 2);
        var now = DateTimeOffset.UtcNow;
        var guild = new Guild
        {
            Name = "Essence Guild",
            OwnerId = characterId,
            Members = { new GuildMember { CharacterId = characterId, Role = GuildRole.Leader } },
            Buildings = { new GuildBuilding { Type = GuildBuildingType.MissionBoard, Level = 3 } }
        };
        db.Guilds.Add(guild);
        var order = new PersonalGuildOrder
        {
            GuildId = guild.Id,
            CharacterId = characterId,
            MissionDefinitionId = Guid.Parse("0171138e-654b-455b-9a81-8b681210ce76"),
            PeriodType = GuildMissionPeriodType.Daily,
            PeriodKey = now.ToString("yyyyMMdd"),
            TargetAmount = 1,
            GeneratedAt = now
        };
        if (existingOrder) db.PersonalGuildOrders.Add(order);
        var weekStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero)
            .AddDays(-((int)now.DayOfWeek + 6) % 7);
        var weekly = new GuildMissionInstance
        {
            GuildId = guild.Id,
            MissionDefinitionId = Guid.Parse("3062cb4d-7c85-494a-89f2-4a9c6d0d1a96"),
            WeekKey = weekStart.ToString("yyyyMMdd"),
            TargetAmount = 100,
            Status = GuildMissionStatus.Active,
            StartedAt = weekStart,
            EndsAt = weekStart.AddDays(7),
            RewardClaimDeadline = weekStart.AddDays(14)
        };
        db.GuildMissionInstances.Add(weekly);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var outbox = new RecordingGameEventOutbox();
        var missions = new GuildMissionService(db, new DefaultGuildContentProvider(), new GuildRepository(db), outbox: outbox);
        var essences = CreateService(db, outbox: outbox, guildMissionService: missions);

        var succeeded = shatterQuantity == 0
            ? (await essences.AbsorbUnboundEssenceAsync(characterId, itemId, default)).Succeeded
            : (await essences.DismantleUnboundEssenceAsync(characterId, itemId, default, shatterQuantity)).Succeeded;
        Assert.True(succeeded);
        await db.SaveChangesAsync();
        var overview = await missions.GetOverviewAsync(characterId, now, default);
        var completed = Assert.Single(overview!.PersonalOrders, candidate => candidate.Definition.Key == "daily.essence_absorption");
        if (existingOrder) Assert.Equal(order.Id, completed.Id);
        Assert.Equal("Absorb or Shatter an Essence.", completed.Definition.Description);
        Assert.Equal(PersonalGuildOrderStatus.Completed, completed.Status);
        Assert.Equal(1, completed.CurrentAmount);
        Assert.True(completed.CanClaimReward);
        Assert.Equal(shatterQuantity == 0 ? 1 : 0, overview.ActiveMission!.CurrentAmount);
        var contribution = Assert.Single(await db.GuildContributionLedgers.ToListAsync());
        Assert.Equal(shatterQuantity == 0 ? 1 : shatterQuantity, contribution.Amount);
        Assert.Single(outbox.EventTypes, type => type == GameEventTypes.GuildMissionProgressed);

        // Duplicate absorption and invalid shatter quantities must not add progress.
        var rejected = shatterQuantity == 0
            ? !(await essences.AbsorbUnboundEssenceAsync(characterId, itemId, default)).Succeeded
            : !(await essences.DismantleUnboundEssenceAsync(characterId, itemId, default, 3)).Succeeded;
        Assert.True(rejected);
        await db.SaveChangesAsync();
        Assert.Single(await db.GuildContributionLedgers.ToListAsync());
        Assert.Single(outbox.EventTypes, type => type == GameEventTypes.GuildMissionProgressed);

        var claim = await missions.ClaimPersonalOrderRewardAsync(characterId, completed.Id, now, default);
        Assert.True(claim.Succeeded);
        await db.SaveChangesAsync();
        var duplicateClaim = await missions.ClaimPersonalOrderRewardAsync(characterId, completed.Id, now, default);
        Assert.False(duplicateClaim.Succeeded);
        Assert.Equal(completed.Reward.GuildFavor, (await db.Characters.SingleAsync()).GuildFavor);
    }
}
