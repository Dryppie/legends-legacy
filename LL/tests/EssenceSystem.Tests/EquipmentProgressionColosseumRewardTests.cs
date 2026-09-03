using Application.UseCases.Inventories.SelectionCrates;
using Application.Interfaces.Services.LL.Colosseum;
using AutoMapper;
using Application.Common.Mappings;
using Application.UseCases.Colosseum.Tournaments;
using Domain.Models.Colosseum;
using Domain.Models.Colosseum.Tournaments;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.LL.Repositories.Colosseum;
using Services.LL.Colosseum;
using Services.LL.Inventories;

namespace EssenceSystem.Tests;

public sealed partial class TournamentGroundsServiceTests
{
    [Theory]
    [InlineData(true)]
    public async Task EquipmentProgression_champion_preview_purchase_and_limits_use_the_same_offer(bool modern)
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());
        character.ArenaProfile.Glory = 1000;
        var inventory = new RecordingInventoryService();
        var catalog = new EquipmentProgressionMarketCatalog();
        var service = new ColosseumService(null!, null!, null!, new ColosseumRepository(db),
            null!, null!, null!, null!,
            new FakeItemBaseRepository([
                new ItemBase { ItemType = ItemType.Resource, Id = "tempered_scrap", Name = "Tempered Scrap", Stackable = true },
            ]), catalog, inventory, new InventoryItemFactory());
        await db.SaveChangesAsync();
        var offer = Assert.Single(await service.GetChampionMarketItemsAsync(character.Id, CancellationToken.None));
        Assert.Equal(modern ? "tempered_scrap" : "tempered_scrap", offer.RewardItemId);
        Assert.Equal(modern ? 2 : 1, offer.RewardItemQuantity);
        Assert.Equal(220, offer.GloryCost);
        var purchase = await service.PurchaseChampionMarketItemAsync(character.Id, offer.Id, 2, CancellationToken.None);
        Assert.NotNull(purchase);
        var reward = Assert.Single(inventory.AddedRewards);
        Assert.Equal(offer.RewardItemId, reward.ItemInstance.ItemBaseId);
        Assert.Equal(offer.RewardItemQuantity * 2, reward.Quantity);
        Assert.Equal(560, character.ArenaProfile.Glory);
        await db.SaveChangesAsync();
        var receipt = await db.ChampionMarketPurchases.SingleAsync();
        Assert.Equal("cache.catalyst_selection", receipt.ItemId);
        Assert.Equal(2, receipt.Quantity);
        Assert.Null(await service.PurchaseChampionMarketItemAsync(character.Id, offer.Id, 1, CancellationToken.None));
        Assert.Equal(560, character.ArenaProfile.Glory);
        Assert.Equal("tempered_scrap", Assert.Single(catalog.GetAll()).RewardItemId);
    }

    [Theory]
    [InlineData(true)]
    public async Task EquipmentProgression_tournament_preview_pending_claim_and_history_agree_and_preserve_old_claims(bool modern)
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());
        var tournament = SeedTournament(db, TournamentStatus.Completed);
        var inventory = new RecordingInventoryService();
        var service = CreateService(db, inventoryService: inventory,
            itemBaseRepository: new FakeItemBaseRepository([
                new ItemBase { ItemType = ItemType.Resource, Id = "tempered_scrap", Name = "Tempered Scrap", Stackable = true },
                new ItemBase { ItemType = ItemType.Resource, Id = BlueprintSelectionBoxCatalog.ItemBaseId, Name = "Blueprint Selection Box", Stackable = true }
            ]));
        var pending = new TournamentRewardGrant
        {
            Id = Guid.NewGuid(), TournamentId = tournament.Id, Tournament = tournament, CharacterId = character.Id,
            RewardKey = "champion", TemperedScrap = 4, BlueprintSelectionBoxes = 1,
            ArenaGlory = 500, Soulstones = 50, SigilFragments = 20,
            CreatedAtUtc = Now, Status = TournamentRewardStatus.Unclaimed
        };
        var old = new TournamentRewardGrant
        {
            Id = Guid.NewGuid(), TournamentId = tournament.Id, Tournament = tournament, CharacterId = character.Id,
            RewardKey = "old", TemperedScrap = 2, CreatedAtUtc = Now.AddDays(-7),
            Status = TournamentRewardStatus.Claimed, ClaimedAtUtc = Now.AddDays(-6)
        };
        db.TournamentRewardGrants.AddRange(pending, old);
        await db.SaveChangesAsync();
        var tiers = await service.GetRewardTiersAsync(character.Id, CancellationToken.None);
        var champion = Assert.Single(tiers, x => x.Key == "champion");
        Assert.Equal(2, champion.TemperedScrap);
        var preview = Assert.Single(await service.GetRewardsAsync(character.Id, tournament.Id, CancellationToken.None), x => x.Id == pending.Id);
        Assert.Equal(modern ? 4 : 0, preview.TemperedScrap);

        var claim = await service.ClaimRewardsAsync(character.Id, tournament.Id, CancellationToken.None);
        Assert.True(claim.Claimed);
        Assert.Equal(preview.TemperedScrap, claim.TemperedScrap);
        Assert.Equal(500, claim.ArenaGlory);
        Assert.Equal(50, claim.Soulstones);
        Assert.Equal(20, claim.SigilFragments);
        Assert.Contains(inventory.AddedRewards, r => r.ItemInstance.ItemBaseId == BlueprintSelectionBoxCatalog.ItemBaseId && r.Quantity == 1);
        Assert.Contains(inventory.AddedRewards, r => r.ItemInstance.ItemBaseId == (modern ? "tempered_scrap" : "tempered_scrap") && r.Quantity == (modern ? 4 : 2));
        Assert.False((await service.ClaimRewardsAsync(character.Id, tournament.Id, CancellationToken.None)).Claimed);

        db.ChangeTracker.Clear();
        // Claimed rewards retain their exact entitlement.
        var historyService = CreateService(db);
        var history = await historyService.GetRewardsAsync(character.Id, tournament.Id, CancellationToken.None);
        var delivered = Assert.Single(history, x => x.Id == pending.Id);
        Assert.Equal(preview.TemperedScrap, delivered.TemperedScrap);
        var oldClaim = Assert.Single(history, x => x.Id == old.Id);
        Assert.Equal(2, oldClaim.TemperedScrap);

        var mapper = new MapperConfiguration(c => c.AddProfile(new MappingProfile()), NullLoggerFactory.Instance).CreateMapper();
        Assert.Equal(delivered.TemperedScrap, mapper.Map<TournamentRewardGrantDto>(delivered).TemperedScrap);
        Assert.Equal(champion.TemperedScrap, mapper.Map<TournamentRewardTierDto>(champion).TemperedScrap);
    }

    [Fact]
    public async Task EquipmentProgression_tournament_automatic_claim_uses_the_owner_cohort_once()
    {
        await using var db = CreateDbContext();
        var oldTournament = SeedTournament(db, TournamentStatus.Completed);
        var starting = SeedTournament(db, TournamentStatus.BracketGenerated);
        starting.StartsAtUtc = Now.AddMinutes(-1);
        var character = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());
        var inventory = new RecordingInventoryService();
        var service = CreateService(db, inventoryService: inventory,
            itemBaseRepository: new FakeItemBaseRepository([new ItemBase { Id = "tempered_scrap", ItemType = ItemType.Resource, Stackable = true }]));
        var reward = new TournamentRewardGrant
        {
            Id = Guid.NewGuid(), TournamentId = oldTournament.Id, Tournament = oldTournament, CharacterId = character.Id,
            RewardKey = "champion", TemperedScrap = 2, CreatedAtUtc = Now.AddDays(-7),
            Status = TournamentRewardStatus.Unclaimed
        };
        db.TournamentRewardGrants.Add(reward);
        await db.SaveChangesAsync();
        await service.AdvanceDueTournamentsAsync(CancellationToken.None);
        Assert.Equal(TournamentRewardStatus.Claimed, reward.Status);
        var item = Assert.Single(inventory.AddedRewards);
        Assert.Equal("tempered_scrap", item.ItemInstance.ItemBaseId);
        Assert.Equal(2, item.Quantity);
        Assert.False((await service.ClaimRewardsAsync(character.Id, oldTournament.Id, CancellationToken.None)).Claimed);
        Assert.Single(inventory.AddedRewards);
    }

    [Fact]
    public async Task EquipmentProgression_tournament_missing_scrap_definition_leaves_entitlement_and_currency_unmodified()
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, rating: 1500, accountId: Guid.NewGuid());
        var tournament = SeedTournament(db, TournamentStatus.Completed);
        var reward = new TournamentRewardGrant
        {
            Id = Guid.NewGuid(), TournamentId = tournament.Id, Tournament = tournament, CharacterId = character.Id,
            RewardKey = "champion", TemperedScrap = 2, ArenaGlory = 500,
            CreatedAtUtc = Now, Status = TournamentRewardStatus.Unclaimed
        };
        db.TournamentRewardGrants.Add(reward);
        await db.SaveChangesAsync();
        var originalGlory = character.ArenaProfile.Glory;
        var service = CreateService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ClaimRewardsAsync(character.Id, tournament.Id, CancellationToken.None));
        Assert.Equal(originalGlory, character.ArenaProfile.Glory);
        Assert.Equal(TournamentRewardStatus.Unclaimed, reward.Status);
    }

    private sealed class EquipmentProgressionMarketCatalog : IChampionMarketCatalog
    {
        public IReadOnlyList<ChampionMarketItem> GetAll() => [new("cache.catalyst_selection", "Catalyst Selection Cache",
            "Choose six catalysts.", "Weekly Cache", 220, 2, null, null, null, true, 1,
            RewardItemId: "tempered_scrap", RewardItemQuantity: 2)];
        public IReadOnlyList<ChampionMarketItem> GetActive(DateTimeOffset now) => GetAll();
        public ChampionMarketItem? GetById(string id) => GetAll().FirstOrDefault(x => x.Id == id);
    }
}
