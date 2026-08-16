using Application.Interfaces.Services.LL.Colosseum;
using Domain.Models.Achievements;
using Domain.Models.Colosseum;
using Domain.Models.Entities.Characters;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Achievements;
using Persistence.LL.Repositories.Colosseum;
using Services.LL.Achievements;
using Services.LL.Colosseum;

namespace EssenceSystem.Tests;

public sealed class ChampionMarketTitleBackfillTests
{
    private const string MarketItemId = "title.bloodied_challenger";
    private const string TitleKey = "title.bloodied_challenger";

    [Fact]
    public async Task Backfill_grants_title_for_purchase_made_before_the_reward_existed()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedTitle(db);
        SeedPurchase(db, characterId);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var grants = await service.BackfillMissingChampionMarketTitleGrantsAsync(CancellationToken.None);
        await db.SaveChangesAsync();

        var grant = Assert.Single(grants);
        Assert.Equal(characterId, grant.CharacterId);
        Assert.Equal(TitleKey, grant.TitleKey);

        var unlock = Assert.Single(await db.PlayerTitleUnlocks.ToListAsync());
        Assert.Equal(accountId, unlock.AccountId);
        Assert.Equal(characterId, unlock.CharacterId);
        Assert.Contains("\"Backfilled\":true", unlock.MetadataJson!);
    }

    [Fact]
    public async Task Backfill_is_idempotent_and_skips_already_unlocked_titles()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedTitle(db);
        SeedPurchase(db, characterId);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.BackfillMissingChampionMarketTitleGrantsAsync(CancellationToken.None);
        await db.SaveChangesAsync();
        var secondRun = await service.BackfillMissingChampionMarketTitleGrantsAsync(CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Empty(secondRun);
        Assert.Single(await db.PlayerTitleUnlocks.ToListAsync());
    }

    [Fact]
    public async Task Backfill_ignores_purchases_that_do_not_reward_a_title()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        SeedCharacter(db, accountId, characterId);
        SeedTitle(db);
        SeedPurchase(db, characterId, itemId: "cache.catalyst_selection");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var grants = await service.BackfillMissingChampionMarketTitleGrantsAsync(CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Empty(grants);
        Assert.Empty(await db.PlayerTitleUnlocks.ToListAsync());
    }

    private static ColosseumService CreateService(LLDbContext db) =>
        new(
            null!,
            null!,
            null!,
            new ColosseumRepository(db),
            null!,
            null!,
            null!,
            null!,
            null!,
            new StubChampionMarketCatalog(),
            null!,
            null!,
            new AchievementService(new AchievementRepository(db)));

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static void SeedCharacter(LLDbContext db, Guid accountId, Guid characterId) =>
        db.Characters.Add(new Character
        {
            Id = characterId,
            UserId = accountId,
            Name = "Hero",
            ImagePath = "player",
            Level = 1
        });

    private static void SeedTitle(LLDbContext db) =>
        db.TitleDefinitions.Add(new TitleDefinition
        {
            Id = Guid.NewGuid(),
            Key = TitleKey,
            Name = "Bloodied Challenger",
            Description = "Purchased from the Champion's Market after reaching Bronze rank.",
            Category = AchievementCategory.Colosseum,
            Rarity = TitleRarity.Mythic,
            Scope = TitleScope.Character,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

    private static void SeedPurchase(LLDbContext db, Guid characterId, string itemId = MarketItemId) =>
        db.ChampionMarketPurchases.Add(new ChampionMarketPurchase
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            ItemId = itemId,
            Quantity = 1,
            GloryCostPaid = 500,
            PurchasedAt = DateTimeOffset.UtcNow.AddDays(-14)
        });

    private sealed class StubChampionMarketCatalog : IChampionMarketCatalog
    {
        private static readonly ChampionMarketItem TitleItem = new(
            Id: MarketItemId,
            Name: "Title: Bloodied Challenger",
            Description: "Unlocks the Bloodied Challenger title.",
            Category: "Title",
            GloryCost: 500,
            WeeklyPurchaseLimit: null,
            LifetimePurchaseLimit: 1,
            RequiredRating: null,
            RequiredRankTier: "bronze",
            IsEnabled: true,
            SortOrder: 20,
            RewardTitleKey: TitleKey);

        private static readonly ChampionMarketItem CacheItem = new(
            Id: "cache.catalyst_selection",
            Name: "Catalyst Selection Cache",
            Description: "A cache.",
            Category: "Weekly Cache",
            GloryCost: 220,
            WeeklyPurchaseLimit: 1,
            LifetimePurchaseLimit: null,
            RequiredRating: null,
            RequiredRankTier: "silver",
            IsEnabled: true,
            SortOrder: 30);

        public IReadOnlyList<ChampionMarketItem> GetAll() => [TitleItem, CacheItem];

        public IReadOnlyList<ChampionMarketItem> GetActive(DateTimeOffset now) => GetAll();

        public ChampionMarketItem? GetById(string itemId) =>
            GetAll().FirstOrDefault(x => x.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));
    }
}
