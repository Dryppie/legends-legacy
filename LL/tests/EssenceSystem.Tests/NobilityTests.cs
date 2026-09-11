using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.MarketPlaces;
using Domain.Models.Nobility;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Nobility;
using Services.LL.Nobility;

namespace EssenceSystem.Tests;

public sealed class NobilityTests
{
    [Theory]
    [InlineData("2027-01-31T12:00:00Z", "2028-01-31T12:00:00Z")]
    [InlineData("2028-02-29T12:00:00Z", "2029-02-28T12:00:00Z")]
    public void Calendar_anchor_makes_individual_and_bulk_redemption_equivalent(string start, string expected)
    {
        var now = DateTimeOffset.Parse(start);
        var single = new NobilityMembership();
        for (var i = 0; i < 12; i++) single.Extend(1, now.AddMinutes(i));
        var bulk = new NobilityMembership();
        bulk.Extend(12, now);
        Assert.Equal(DateTimeOffset.Parse(expected), single.Coverage.Single().EndsAt);
        Assert.Equal(bulk.Coverage.Single().EndsAt, single.Coverage.Single().EndsAt);
        Assert.Null(single.At(DateTimeOffset.Parse(expected)));
    }

    [Fact]
    public async Task Alpha_grant_does_not_activate_or_award_cash_support_and_is_idempotent()
    {
        await using var fixture = await Fixture.Create();
        var operation = Guid.NewGuid();
        var first = await fixture.Service.GrantAlphaAsync("operator", fixture.Character.Id, operation, 12, "Alpha testing", default);
        Assert.True(first.IsSuccess);
        await fixture.Db.SaveChangesAsync();
        Assert.True((await fixture.Service.GrantAlphaAsync("operator", fixture.Character.Id, operation, 12, "Alpha testing", default)).IsSuccess);
        var status = await fixture.Status();
        Assert.Equal(12, status.AvailableSignets);
        Assert.False(status.IsNoble);
        Assert.False(status.HasSupportHistory);
        Assert.Single(fixture.Db.Set<SignetIssuance>());
        Assert.Equal(12, await fixture.Db.Set<SignetUnit>().CountAsync());
        Assert.True((await fixture.Service.GrantAlphaAsync("operator", fixture.Character.Id, operation, 1, "Alpha testing", default)).IsConflict);
    }

    [Fact]
    public async Task Redemption_consumes_exact_units_and_duplicate_request_does_not_extend_twice()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Grant(3);
        var preview = (await fixture.Service.PreviewAsync(fixture.Character.UserId, fixture.Character.Id, 2, default)).Data!;
        var operation = Guid.NewGuid();
        var result = await fixture.Service.RedeemAsync(fixture.Character.UserId, fixture.Character.Id, operation,
            preview.MembershipVersion, preview.UnitIds, DateOnly.FromDateTime(preview.ExpiresAt.UtcDateTime), default);
        Assert.True(result.IsSuccess);
        await fixture.Db.SaveChangesAsync();
        fixture.Clock.Now = fixture.Clock.Now.AddHours(1);
        var replay = await fixture.Service.RedeemAsync(fixture.Character.UserId, fixture.Character.Id, operation,
            preview.MembershipVersion, preview.UnitIds, DateOnly.FromDateTime(preview.ExpiresAt.UtcDateTime), default);
        Assert.Equal(result.Data!.ExpiresAt, replay.Data!.ExpiresAt);
        Assert.Equal(1, (await fixture.Status()).AvailableSignets);
        Assert.Equal(168, (await fixture.Status()).Benefits.OfflineHours);
    }

    [Fact]
    public async Task Stale_preview_and_duplicate_units_do_not_consume_signets()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Grant(2);
        var preview = (await fixture.Service.PreviewAsync(fixture.Character.UserId, fixture.Character.Id, 1, default)).Data!;
        var stale = await fixture.Service.RedeemAsync(fixture.Character.UserId, fixture.Character.Id, Guid.NewGuid(), Guid.NewGuid(),
            preview.UnitIds, DateOnly.FromDateTime(preview.ExpiresAt.UtcDateTime), default);
        Assert.True(stale.IsConflict);
        var duplicated = await fixture.Service.RedeemAsync(fixture.Character.UserId, fixture.Character.Id, Guid.NewGuid(), Guid.Empty,
            [preview.UnitIds[0], preview.UnitIds[0]], DateOnly.FromDateTime(preview.ExpiresAt.UtcDateTime), default);
        Assert.False(duplicated.IsSuccess);
        Assert.Equal(2, (await fixture.Status()).AvailableSignets);
    }

    [Fact]
    public async Task Listed_units_cannot_be_redeemed_and_resale_preserves_issuance()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Grant(2);
        var repository = new NobilityRepository(fixture.Db);
        var trading = new SignetTradingService(repository, fixture.Clock);
        var item = await fixture.Db.InventoryItems.Include(x => x.ItemInstance).SingleAsync();
        var listing = new MarketPlaceListing { Id = Guid.NewGuid(), SellerId = fixture.Character.Id,
            ItemInstance = item.ItemInstance, ItemInstanceId = item.ItemInstanceId, Quantity = 2 };
        await trading.ReserveAsync(listing, default);
        await fixture.Db.SaveChangesAsync();
        Assert.False((await fixture.Service.PreviewAsync(fixture.Character.UserId, fixture.Character.Id, 1, default)).IsSuccess);
        var buyerUser = AppUser.Register("buyer", "buyer@example.com", "hash");
        var buyer = new Character { Id = Guid.NewGuid(), UserId = buyerUser.Id, User = buyerUser, Name = "buyer" };
        fixture.Db.Characters.Add(buyer);
        await fixture.Db.SaveChangesAsync();
        await trading.TradeAsync(new MarketPlaceOrder { Id = Guid.NewGuid(), SellerId = fixture.Character.Id,
            BuyerId = buyer.Id, ItemBaseId = "signet", Quantity = 1 }, listing.Id, default);
        listing.Quantity = 1;
        await trading.ReleaseAsync(listing, default);
        await fixture.Db.SaveChangesAsync();
        Assert.Equal(1, (await fixture.Status()).AvailableSignets);
        Assert.Single(await repository.GetUnitsAsync(buyer.Id, SignetState.Available, default));
        Assert.Single(await fixture.Db.Set<SignetUnit>().Select(x => x.IssuanceId).Distinct().ToListAsync());
        await trading.TradeAsync(new MarketPlaceOrder { Id = Guid.NewGuid(), SellerId = buyer.Id,
            BuyerId = fixture.Character.Id, ItemBaseId = "signet", Quantity = 1 }, null, default);
        await fixture.Db.SaveChangesAsync();
        Assert.Equal(2, (await fixture.Status()).AvailableSignets);
        Assert.False(await repository.HasCashSupportAsync(buyer.UserId, default));
    }

    [Fact]
    public async Task Daily_rewards_are_offline_once_per_covered_day_and_survive_expiry()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Grant(1);
        var preview = (await fixture.Service.PreviewAsync(fixture.Character.UserId, fixture.Character.Id, 1, default)).Data!;
        await fixture.Service.RedeemAsync(fixture.Character.UserId, fixture.Character.Id, Guid.NewGuid(), preview.MembershipVersion,
            preview.UnitIds, DateOnly.FromDateTime(preview.ExpiresAt.UtcDateTime), default);
        await fixture.Db.SaveChangesAsync();
        Assert.Equal(0, await fixture.Service.ApplyDailyRewardsAsync(fixture.Character.UserId, fixture.Character.Id, default));
        fixture.Clock.Now = preview.ExpiresAt.AddDays(5);
        var total = await fixture.Service.ApplyDailyRewardsAsync(fixture.Character.UserId, fixture.Character.Id, default);
        await fixture.Db.SaveChangesAsync();
        total += await fixture.Service.ApplyDailyRewardsAsync(fixture.Character.UserId, fixture.Character.Id, default);
        await fixture.Db.SaveChangesAsync();
        Assert.Equal(29, total); // January 31 noon through February 28 noon, including both partial UTC days.
        Assert.Equal(290, fixture.Character.Soulstones);
        Assert.Equal(58, (await fixture.Db.InventoryItems.Include(x => x.ItemInstance)
            .SingleAsync(x => x.ItemInstance.ItemBaseId == SigilFragmentItem.ItemBaseId)).Quantity);
        Assert.Equal(0, await fixture.Service.ApplyDailyRewardsAsync(fixture.Character.UserId, fixture.Character.Id, default));
    }

    [Fact]
    public void Retention_preserves_expiry_window_and_does_not_restore_expired_free_gaps()
    {
        var start = DateTimeOffset.Parse("2027-01-31T12:00:00Z");
        var end = start.AddMonths(1);
        var coverage = new NobilityCoverage { StartsAt = start, EndsAt = end };
        var windows = NobilityRetention.Windows([coverage], end.AddDays(3));
        Assert.Contains(windows, x => x.From == end.AddDays(-7) && x.Until == end);
        Assert.DoesNotContain(windows, x => x.From <= end.AddHours(12) && x.Until > end.AddHours(12));
        Assert.DoesNotContain(windows, x => x.From <= start.AddDays(-2) && x.Until > start.AddDays(-2));
        Assert.Contains(windows, x => x.From == end.AddDays(2));
    }

    [Theory]
    [InlineData(19, 0)]
    [InlineData(20, 1)]
    [InlineData(101, 5)]
    public void Bonus_rounds_once_on_base_xp(int experience, int expected) =>
        Assert.Equal(expected, NobilityBenefits.Noble.AdditionalExperience(experience));

    [Fact]
    public async Task Arena_uses_earning_time_cap_without_activation_refill_or_expiry_clamping()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Activate();
        var expiry = (await fixture.Status()).ExpiresAt!.Value;
        var tickets = new Domain.Models.Colosseum.ArenaTicketStatus
        { CharacterId = fixture.Character.Id, CurrentTickets = 5, LastTicketUpdate = fixture.Clock.Now };
        fixture.Db.ArenaTicketStatus.Add(tickets);
        await fixture.Db.SaveChangesAsync();
        var service = new Services.LL.Colosseum.ColosseumService(null!, null!, null!,
            new Persistence.LL.Repositories.Colosseum.ColosseumRepository(fixture.Db),
            null!, null!, null!, null!, null!, null!, null!, null!, nobility: fixture.Service, time: fixture.Clock);
        Assert.Equal(5, (await service.GetArenaTicketStatusAsync(fixture.Character.Id, default)).CurrentTickets);
        Assert.Equal(8, tickets.MaxTickets);
        fixture.Clock.Now = expiry.AddHours(1);
        Assert.Equal(8, (await service.GetArenaTicketStatusAsync(fixture.Character.Id, default)).CurrentTickets);
        Assert.Equal(5, tickets.MaxTickets);
        tickets.CurrentTickets = 4;
        fixture.Clock.Now = expiry.AddHours(10);
        Assert.Equal(5, (await service.GetArenaTicketStatusAsync(fixture.Character.Id, default)).CurrentTickets);
    }

    [Theory]
    [InlineData(0, -1, 110)]
    [InlineData(74893, -1, 107)]
    [InlineData(75000, -1, 105)]
    [InlineData(0, 0, 105)]
    public async Task Mastery_bonus_uses_completion_time_and_does_not_exceed_level_cap(long existing, int secondsFromExpiry, long awarded)
    {
        await using var fixture = await Fixture.Create();
        await fixture.Activate();
        var expiry = (await fixture.Status()).ExpiresAt!.Value;
        fixture.Db.CharacterDungeonMasteries.Add(new Domain.Models.Dungeons.Mastery.CharacterDungeonMastery
        { CharacterId = fixture.Character.Id, DungeonDefinitionId = "goblin_mines", Experience = existing });
        await fixture.Db.SaveChangesAsync();
        fixture.Clock.Now = expiry.AddDays(2);
        var service = new Services.LL.Dungeons.DungeonMasteryService(
            new Persistence.LL.Repositories.Dungeons.CharacterDungeonMasteryRepository(fixture.Db), fixture.Service);
        var run = new Domain.Models.Dungeons.Runs.DungeonRun
        { Id = Guid.NewGuid(), CharacterId = fixture.Character.Id, DungeonDefinitionId = "goblin_mines_i",
            Status = Domain.Models.Dungeons.Runs.DungeonRunStatus.Completed, CompletedAt = expiry.AddSeconds(secondsFromExpiry), Rooms = [] };
        var reward = await service.AwardCompletionAsync(run, default);
        Assert.Equal(awarded, reward.ExperienceAwarded);
        Assert.Equal(0, (await service.AwardCompletionAsync(run, default)).ExperienceAwarded);
    }

    [Fact]
    public async Task Preset_entitlement_uses_historical_combat_scope_and_restores_current_time()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Activate();
        var earnedAt = fixture.Clock.Now;
        fixture.Clock.Now = (await fixture.Status()).ExpiresAt!.Value.AddDays(1);
        var limits = new Services.LL.Essences.EssenceLoadoutLimitService(fixture.Service, fixture.Clock);
        Assert.Equal(3, await limits.GetLoadoutLimitAsync(fixture.Character.Id, default));
        using (fixture.Service.EvaluateCombatAt(earnedAt))
            Assert.Equal(6, await limits.GetLoadoutLimitAsync(fixture.Character.Id, default));
        Assert.Equal(3, await limits.GetLoadoutLimitAsync(fixture.Character.Id, default));
    }

    [Fact]
    public async Task Generic_consumption_cannot_reduce_a_signet_stack_without_a_redemption_receipt()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Grant(2);
        var row = await fixture.Db.InventoryItems.SingleAsync();
        var inventory = new Services.LL.Inventories.InventoryService(
            new Persistence.LL.Repositories.Inventories.InventoryRepository(fixture.Db));
        Assert.False(await inventory.TryConsumeInventoryItemAsync(fixture.Character.Id, row.ItemInstanceId, default));
        Assert.Equal(2, row.Quantity);
        Assert.Equal(2, (await fixture.Status()).AvailableSignets);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Badge_choice_persists_and_public_appearance_expires(bool showBadge)
    {
        await using var fixture = await Fixture.Create();
        await fixture.Activate();
        Assert.True((await fixture.Service.SetAppearanceAsync(fixture.Character.UserId,
            fixture.Character.Id, showBadge, default)).IsSuccess);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();
        var status = await fixture.Status();
        Assert.Equal(showBadge, status.ShowBadge);
        var handler = new Application.UseCases.Nobility.Queries.GetNobilityAppearance.GetNobilityAppearanceQueryHandler(
            new NobilityRepository(fixture.Db), fixture.Clock);
        var query = new Application.UseCases.Nobility.Queries.GetNobilityAppearance.GetNobilityAppearanceQuery(fixture.Character.Id);
        var appearance = await handler.Handle(query, default);
        Assert.Equal(showBadge, appearance.ShowBadge);
        Assert.Equal(status.ExpiresAt, appearance.ExpiresAt);
        fixture.Clock.Now = status.ExpiresAt!.Value;
        var expired = await handler.Handle(query, default);
        Assert.False(expired.ShowBadge);
        Assert.Null(expired.ExpiresAt);
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.Parse("2027-01-31T12:00:00Z");
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public LLDbContext Db = new(new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public Clock Clock = new();
        public Character Character = null!;
        public NobilityService Service = null!;
        public static async Task<Fixture> Create()
        {
            var fixture = new Fixture();
            var user = AppUser.Register("noble", "noble@example.com", "hash");
            fixture.Character = new Character { Id = Guid.NewGuid(), UserId = user.Id, User = user, Name = "noble" };
            fixture.Db.Characters.Add(fixture.Character);
            fixture.Db.ItemBases.AddRange(new MiscItemBase { Id = "signet", Name = "Signet", Stackable = true },
                new ItemBase { Id = SigilFragmentItem.ItemBaseId, Name = "Sigil Fragment", Stackable = true, ItemType = ItemType.Resource });
            await fixture.Db.SaveChangesAsync();
            fixture.Service = new NobilityService(new NobilityRepository(fixture.Db), fixture.Clock);
            return fixture;
        }
        public async Task Grant(int quantity)
        {
            Assert.True((await Service.GrantAlphaAsync("operator", Character.Id, Guid.NewGuid(), quantity, "Alpha testing", default)).IsSuccess);
            await Db.SaveChangesAsync();
        }
        public async Task Activate()
        {
            await Grant(1);
            var preview = (await Service.PreviewAsync(Character.UserId, Character.Id, 1, default)).Data!;
            Assert.True((await Service.RedeemAsync(Character.UserId, Character.Id, Guid.NewGuid(), preview.MembershipVersion,
                preview.UnitIds, DateOnly.FromDateTime(preview.ExpiresAt.UtcDateTime), default)).IsSuccess);
            await Db.SaveChangesAsync();
        }
        public Task<Application.Interfaces.Services.LL.Nobility.NobilityStatus> Status() => Service.GetStatusAsync(Character.UserId, Character.Id, default);
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
