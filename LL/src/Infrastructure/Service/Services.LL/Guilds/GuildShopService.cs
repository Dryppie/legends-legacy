using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Guilds;
using Domain.Models.Achievements;
using Domain.Models.Economy;
using Domain.Models.Entities.Characters;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;
using Domain.Models.Guilds.Shop;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Services.LL.Inventories;
using Services.LL.Interfaces;

namespace Services.LL.Guilds;

public class GuildShopService : IGuildShopService
{
    private const string CommonCatalystRotationGroup = "common-catalysts";
    private const string RareCatalystRotationGroup = "rare-catalysts";
    private const string BlueprintRotationGroup = "rare-blueprints";

    private readonly IDbContext _context;
    private readonly IEconomyLedgerRepository _economyLedger;
    private readonly IInventoryItemFactory _inventoryItemFactory;
    private readonly IReadOnlyList<GuildShopItemDefinition> _items;

    public GuildShopService(
        IDbContext context,
        IEconomyLedgerRepository economyLedger)
        : this(context, new DefaultGuildContentProvider(), new InventoryItemFactory(), economyLedger)
    {
    }

    public GuildShopService(
        IDbContext context,
        IGuildContentProvider content,
        IEconomyLedgerRepository economyLedger)
        : this(context, content, new InventoryItemFactory(), economyLedger)
    {
    }

    public GuildShopService(
        IDbContext context,
        IGuildContentProvider content,
        IInventoryItemFactory inventoryItemFactory,
        IEconomyLedgerRepository economyLedger)
    {
        _context = context;
        _inventoryItemFactory = inventoryItemFactory;
        _economyLedger = economyLedger;
        _items = content.ShopItems;
    }

    public async Task<GuildShopOverviewDto?> GetOverviewAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(characterId, now, cancellationToken);
        return state is null ? null : BuildOverview(state.Value, now);
    }

    public async Task<GuildOperationResult<GuildShopPurchaseResult>> PurchaseAsync(Guid characterId, string itemKey, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(characterId, now, cancellationToken);
        if (state is null) return GuildOperationResult<GuildShopPurchaseResult>.Fail("You are not in a guild.");

        var definition = GetActiveItems(state.Value).FirstOrDefault(x => x.Key == itemKey);
        if (definition is null) return GuildOperationResult<GuildShopPurchaseResult>.Fail("Guild shop item was not found.");

        var lockedReason = GetLockedReason(state.Value, definition, now);
        if (lockedReason is not null)
        {
            return GuildOperationResult<GuildShopPurchaseResult>.Fail(lockedReason);
        }

        var rewardLockedReason = await GetRewardLockedReasonAsync(state.Value.Character, definition, cancellationToken);
        if (rewardLockedReason is not null)
        {
            return GuildOperationResult<GuildShopPurchaseResult>.Fail(rewardLockedReason);
        }

        if (state.Value.Character.GuildFavor < definition.GuildFavorCost)
            return GuildOperationResult<GuildShopPurchaseResult>.Fail("Not enough Guild Favor.");

        state.Value.Character.GuildFavor -= definition.GuildFavorCost;
        var inventoryItemsGranted = new List<Domain.Models.Inventories.InventoryItem>();
        foreach (var reward in definition.Rewards)
        {
            inventoryItemsGranted.AddRange(
                await ApplyRewardAsync(state.Value.Character, reward, now, cancellationToken));
        }

        var purchase = state.Value.Purchases.FirstOrDefault(x => x.ShopItemKey == definition.Key && x.PeriodKey == state.Value.WeeklyPeriodKey);
        if (purchase is null)
        {
            purchase = new GuildShopPurchase
            {
                GuildId = state.Value.Guild.Id,
                CharacterId = characterId,
                ShopItemKey = definition.Key,
                StockType = definition.StockType,
                PeriodKey = state.Value.WeeklyPeriodKey,
                Quantity = 1,
                PurchasedAt = now
            };
            _context.GuildShopPurchases.Add(purchase);
            state.Value.Purchases.Add(purchase);
        }
        else
        {
            purchase.Quantity++;
            purchase.PurchasedAt = now;
        }

        AddActivityLog(
            state.Value.Guild,
            GuildActivityLogType.ShopItemPurchased,
            characterId,
            $"{definition.Name} purchased from the guild shop.",
            now);

        return GuildOperationResult<GuildShopPurchaseResult>.Success(
            new GuildShopPurchaseResult(BuildOverview(state.Value, now), inventoryItemsGranted));
    }

    private async Task<ShopState?> LoadStateAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(x => x.Members)
            .Include(x => x.Buildings)
            .FirstOrDefaultAsync(x => x.Members.Select(m => m.CharacterId).Contains(characterId), cancellationToken);
        if (guild is null) return null;

        var member = guild.Members.FirstOrDefault(x => x.CharacterId == characterId);
        if (member is null) return null;

        var character = await _context.Characters.FirstOrDefaultAsync(x => x.Id == characterId, cancellationToken);
        if (character is null) return null;

        var weeklyPeriod = GetWeek(now);
        var purchases = await _context.GuildShopPurchases
            .Where(x => x.GuildId == guild.Id && x.CharacterId == characterId && x.PeriodKey == weeklyPeriod.Key)
            .ToListAsync(cancellationToken);
        return new ShopState(guild, character, purchases, weeklyPeriod.Key, weeklyPeriod.EndsAt);
    }

    private GuildShopOverviewDto BuildOverview(ShopState state, DateTimeOffset now) =>
        new(
            state.Guild.Id,
            state.Character.GuildFavor,
            state.WeeklyPeriodKey,
            state.NextWeeklyResetAt,
            GetActiveItems(state).Select(item =>
            {
                var purchased = state.Purchases.FirstOrDefault(x => x.ShopItemKey == item.Key && x.PeriodKey == state.WeeklyPeriodKey)?.Quantity ?? 0;
                var lockedReason = GetLockedReason(state, item, now);
                return new GuildShopItemDto(
                    item.Key,
                    item.Name,
                    item.Description,
                    item.StockType,
                    item.GuildFavorCost,
                    item.WeeklyLimit,
                    purchased,
                    item.RequiredMarketOfficeLevel,
                    item.RotatesWeekly,
                    item.Rewards,
                    lockedReason is null,
                    lockedReason);
            }).ToList());

    private IReadOnlyList<GuildShopItemDefinition> GetActiveItems(ShopState state)
    {
        var fixedItems = _items.Where(x => !x.RotatesWeekly);
        var marketOfficeLevel = GetMarketOfficeLevel(state.Guild);
        var commonCatalystItems = GuildContentHelpers.PickWeeklyRotation(
            _items.Where(x =>
                x.RotatesWeekly
                && x.StockType == GuildShopStockType.Common
                && string.Equals(x.RotationGroup, CommonCatalystRotationGroup, StringComparison.OrdinalIgnoreCase)),
            state.WeeklyPeriodKey,
            count: 2,
            x => x.Key);
        var rareCatalystItems = GuildContentHelpers.PickWeeklyRotation(
            _items.Where(x =>
                x.RotatesWeekly
                && x.StockType == GuildShopStockType.Rare
                && string.Equals(x.RotationGroup, RareCatalystRotationGroup, StringComparison.OrdinalIgnoreCase)),
            state.WeeklyPeriodKey,
            count: marketOfficeLevel >= 5 ? 2 : 1,
            x => x.Key);
        var rareBlueprintItems = GuildContentHelpers.PickWeeklyRotation(
            _items.Where(x =>
                x.RotatesWeekly
                && x.StockType == GuildShopStockType.Rare
                && string.Equals(x.RotationGroup, BlueprintRotationGroup, StringComparison.OrdinalIgnoreCase)),
            state.WeeklyPeriodKey,
            count: 1,
            x => x.Key);
        return fixedItems
            .Concat(commonCatalystItems)
            .Concat(rareCatalystItems)
            .Concat(rareBlueprintItems)
            .OrderBy(x => x.StockType)
            .ThenBy(x => x.RequiredMarketOfficeLevel)
            .ThenBy(x => x.Key)
            .ToList();
    }

    private static string? GetLockedReason(ShopState state, GuildShopItemDefinition item, DateTimeOffset now)
    {
        var marketOfficeLevel = GetMarketOfficeLevel(state.Guild);
        if (marketOfficeLevel < item.RequiredMarketOfficeLevel)
            return $"Requires Market Office level {item.RequiredMarketOfficeLevel}.";

        var purchased = state.Purchases.FirstOrDefault(x => x.ShopItemKey == item.Key && x.PeriodKey == state.WeeklyPeriodKey)?.Quantity ?? 0;
        if (item.WeeklyLimit > 0 && purchased >= item.WeeklyLimit)
            return "Weekly purchase limit reached.";

        if (state.Character.GuildFavor < item.GuildFavorCost)
            return "Not enough Guild Favor.";

        return null;
    }

    private static int GetMarketOfficeLevel(Guild guild) =>
        guild.Buildings.FirstOrDefault(x => x.Type == GuildBuildingType.MarketOffice)?.Level ?? 0;

    private async Task<string?> GetRewardLockedReasonAsync(
        Character character,
        GuildShopItemDefinition item,
        CancellationToken cancellationToken)
    {
        foreach (var reward in item.Rewards)
        {
            if (reward.Type is GuildShopRewardType.Item && string.IsNullOrWhiteSpace(reward.Key))
                return $"{item.Name} has an invalid item reward.";
            if (reward.Type is GuildShopRewardType.Title && string.IsNullOrWhiteSpace(reward.Key))
                return $"{item.Name} has an invalid title reward.";
            if (reward.Type is GuildShopRewardType.Item && reward.Amount > int.MaxValue)
                return $"{item.Name} grants too many item copies.";

            if (reward.Type == GuildShopRewardType.Item)
            {
                var itemExists = await _context.ItemBases.AnyAsync(x => x.Id == reward.Key, cancellationToken);
                if (!itemExists) return $"Reward item '{reward.Key}' was not found.";
            }

            if (reward.Type == GuildShopRewardType.Title)
            {
                var title = await _context.TitleDefinitions
                    .FirstOrDefaultAsync(x => x.Key == reward.Key && x.IsActive, cancellationToken);
                if (title is null) return $"Reward title '{reward.Key}' was not found.";

                var alreadyUnlocked = _context.PlayerTitleUnlocks.Local.Any(x =>
                        x.AccountId == character.UserId &&
                        x.CharacterId == character.Id &&
                        x.TitleDefinitionId == title.Id)
                    || await _context.PlayerTitleUnlocks.AnyAsync(x =>
                        x.AccountId == character.UserId &&
                        x.CharacterId == character.Id &&
                        x.TitleDefinitionId == title.Id,
                        cancellationToken);
                if (alreadyUnlocked) return $"Title '{title.Name}' is already unlocked.";
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<Domain.Models.Inventories.InventoryItem>> ApplyRewardAsync(
        Character character,
        GuildShopRewardDto reward,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        switch (reward.Type)
        {
            case GuildShopRewardType.Cinders:
                character.Cinders += reward.Amount;
                return [];
            case GuildShopRewardType.Soulstones:
                character.Soulstones += reward.Amount;
                return [];
            case GuildShopRewardType.FateEcho:
                character.FateEcho += reward.Amount;
                return [];
            case GuildShopRewardType.SigilFragments:
                character.SigilFragments += reward.Amount;
                return [];
            case GuildShopRewardType.Item:
                return await ApplyItemRewardAsync(character.Id, reward, cancellationToken);
            case GuildShopRewardType.Title:
                await ApplyTitleRewardAsync(character, reward, now, cancellationToken);
                return [];
            default:
                return [];
        }
    }

    private async Task<IReadOnlyList<Domain.Models.Inventories.InventoryItem>> ApplyItemRewardAsync(
        Guid characterId,
        GuildShopRewardDto reward,
        CancellationToken cancellationToken)
    {
        var itemBase = await _context.ItemBases.FirstAsync(x => x.Id == reward.Key, cancellationToken);
        var items = _inventoryItemFactory.CreateForQuantity(itemBase, checked((int)reward.Amount), characterId);
        var acquiredAt = DateTimeOffset.UtcNow;
        foreach (var item in items)
        {
            item.ItemInstance.AcquiredAtUtc = acquiredAt;
            item.ItemInstance.AcquisitionSource = ItemAcquisitionSources.GuildShop;
            if (_context.GetEntry(item.ItemInstance).State == EntityState.Detached)
            {
                await _context.ItemInstances.AddAsync(item.ItemInstance, cancellationToken);
            }

            await _context.InventoryItems.AddAsync(item, cancellationToken);
        }
        await _economyLedger.RecordItemAcquisitionsAsync(
            characterId,
            items,
            ItemAcquisitionSources.GuildShop,
            acquiredAt,
            cancellationToken);

        return items;
    }

    private async Task ApplyTitleRewardAsync(
        Character character,
        GuildShopRewardDto reward,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var title = await _context.TitleDefinitions.FirstAsync(x => x.Key == reward.Key && x.IsActive, cancellationToken);
        _context.PlayerTitleUnlocks.Add(new PlayerTitleUnlock
        {
            Id = Guid.NewGuid(),
            AccountId = character.UserId,
            CharacterId = character.Id,
            TitleDefinitionId = title.Id,
            TitleDefinition = title,
            UnlockedAt = now,
            MetadataJson = "{\"source\":\"guild-shop\"}"
        });
    }

    private void AddActivityLog(
        Guild guild,
        GuildActivityLogType type,
        Guid? characterId,
        string message,
        DateTimeOffset now)
    {
        _context.GuildActivityLogs.Add(new GuildActivityLog
        {
            GuildId = guild.Id,
            Type = type,
            CharacterId = characterId,
            Message = message,
            CreatedAt = now
        });
    }

    private static WeekPeriod GetWeek(DateTimeOffset now)
    {
        var utcDate = now.UtcDateTime.Date;
        var daysSinceMonday = ((int)utcDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var start = new DateTimeOffset(utcDate.AddDays(-daysSinceMonday), TimeSpan.Zero);
        return new WeekPeriod(start.ToString("yyyyMMdd"), start.AddDays(7));
    }

    private readonly record struct ShopState(
        Guild Guild,
        Character Character,
        List<GuildShopPurchase> Purchases,
        string WeeklyPeriodKey,
        DateTimeOffset NextWeeklyResetAt);

    private sealed record WeekPeriod(string Key, DateTimeOffset EndsAt);
}
