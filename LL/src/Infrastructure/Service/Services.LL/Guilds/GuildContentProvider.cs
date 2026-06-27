using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces.Services.LL.Guilds;
using Domain.Models.Guilds.Buildings;
using Domain.Models.Guilds.Missions;
using Domain.Models.Guilds.Shop;
using Microsoft.Extensions.Configuration;

namespace Services.LL.Guilds;

public interface IGuildContentProvider
{
    IReadOnlyList<GuildBuildingDefinition> Buildings { get; }
    IReadOnlyList<GuildMissionDefinition> WeeklyMissions { get; }
    IReadOnlyList<GuildMissionDefinition> DailyOrders { get; }
    IReadOnlyList<GuildShopItemDefinition> ShopItems { get; }
}

public sealed class JsonGuildContentProvider : IGuildContentProvider
{
    public JsonGuildContentProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options,
        IGuildContentValidator? validator = null)
    {
        validator ??= new GuildContentValidator();
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "guild-content.json");
        if (!File.Exists(path))
        {
            Apply(Validate(GuildContentDefaults.Document, validator));
            return;
        }

        var document = JsonSerializer.Deserialize<GuildContentDocument>(File.ReadAllText(path), options)
            ?? GuildContentDefaults.Document;
        Apply(Validate(document, validator));
    }

    public IReadOnlyList<GuildBuildingDefinition> Buildings { get; private set; } = [];
    public IReadOnlyList<GuildMissionDefinition> WeeklyMissions { get; private set; } = [];
    public IReadOnlyList<GuildMissionDefinition> DailyOrders { get; private set; } = [];
    public IReadOnlyList<GuildShopItemDefinition> ShopItems { get; private set; } = [];

    private void Apply(GuildContentDocument document)
    {
        Buildings = document.Buildings.Count == 0 ? GuildContentDefaults.Document.Buildings : document.Buildings;
        WeeklyMissions = document.WeeklyMissions.Count == 0 ? GuildContentDefaults.Document.WeeklyMissions : document.WeeklyMissions;
        DailyOrders = document.DailyOrders.Count == 0 ? GuildContentDefaults.Document.DailyOrders : document.DailyOrders;
        ShopItems = document.ShopItems.Count == 0 ? GuildContentDefaults.Document.ShopItems : document.ShopItems;
    }

    private static GuildContentDocument Validate(GuildContentDocument document, IGuildContentValidator validator)
    {
        var result = validator.Validate(document);
        if (result.IsValid)
        {
            return document;
        }

        throw new InvalidOperationException($"Guild content validation failed: {string.Join("; ", result.Errors)}");
    }
}

public sealed class DefaultGuildContentProvider : IGuildContentProvider
{
    public IReadOnlyList<GuildBuildingDefinition> Buildings => GuildContentDefaults.Document.Buildings;
    public IReadOnlyList<GuildMissionDefinition> WeeklyMissions => GuildContentDefaults.Document.WeeklyMissions;
    public IReadOnlyList<GuildMissionDefinition> DailyOrders => GuildContentDefaults.Document.DailyOrders;
    public IReadOnlyList<GuildShopItemDefinition> ShopItems => GuildContentDefaults.Document.ShopItems;
}

public sealed class GuildContentDocument
{
    public IReadOnlyList<GuildBuildingDefinition> Buildings { get; init; } = [];
    public IReadOnlyList<GuildMissionDefinition> WeeklyMissions { get; init; } = [];
    public IReadOnlyList<GuildMissionDefinition> DailyOrders { get; init; } = [];
    public IReadOnlyList<GuildShopItemDefinition> ShopItems { get; init; } = [];
}

public interface IGuildContentValidator
{
    GuildContentValidationResult Validate(GuildContentDocument document);
}

public sealed record GuildContentValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public sealed class GuildContentValidator : IGuildContentValidator
{
    public GuildContentValidationResult Validate(GuildContentDocument document)
    {
        var errors = new List<string>();
        ValidateBuildings(document.Buildings, errors);
        ValidateMissions(document.WeeklyMissions, "weekly mission", errors);
        ValidateMissions(document.DailyOrders, "daily order", errors);
        ValidateShopItems(document.ShopItems, errors);
        return new GuildContentValidationResult(errors);
    }

    private static void ValidateBuildings(IReadOnlyList<GuildBuildingDefinition> buildings, List<string> errors)
    {
        if (buildings.Count == 0)
        {
            errors.Add("At least one building definition is required");
            return;
        }

        AddDuplicateErrors(buildings.Select(x => x.Type.ToString()), "building type", errors);
        if (!buildings.Any(x => x.Type == GuildBuildingType.GuildHall))
        {
            errors.Add("GuildHall building definition is required");
        }

        foreach (var building in buildings)
        {
            if (string.IsNullOrWhiteSpace(building.Name)) errors.Add($"{building.Type} building name is required");
            if (building.MaxLevel <= 0) errors.Add($"{building.Type} max level must be positive");
            if (building.RequiredGuildHallLevel <= 0) errors.Add($"{building.Type} required Guild Hall level must be positive");
            if (building.BaseCost < 0) errors.Add($"{building.Type} base cost cannot be negative");
            if (building.UpgradeCostStep < 0) errors.Add($"{building.Type} upgrade cost step cannot be negative");
            if (building.BaseHours <= 0) errors.Add($"{building.Type} base hours must be positive");
            if (building.Benefits.Count == 0) errors.Add($"{building.Type} must define at least one benefit");

            foreach (var benefit in building.Benefits)
            {
                if (benefit.Level <= 0 || benefit.Level > building.MaxLevel)
                    errors.Add($"{building.Type} benefit '{benefit.Title}' has an invalid level");
                if (string.IsNullOrWhiteSpace(benefit.Title))
                    errors.Add($"{building.Type} benefit title is required");
                if (string.IsNullOrWhiteSpace(benefit.Description))
                    errors.Add($"{building.Type} benefit '{benefit.Title}' description is required");
            }
        }
    }

    private static void ValidateMissions(IReadOnlyList<GuildMissionDefinition> missions, string label, List<string> errors)
    {
        if (missions.Count == 0)
        {
            errors.Add($"At least one {label} definition is required");
            return;
        }

        AddDuplicateErrors(missions.Select(x => x.Id.ToString()), $"{label} id", errors);
        AddDuplicateErrors(missions.Select(x => x.Key), $"{label} key", errors);
        foreach (var mission in missions)
        {
            if (mission.Id == Guid.Empty) errors.Add($"{label} '{mission.Key}' id is required");
            if (string.IsNullOrWhiteSpace(mission.Key)) errors.Add($"{label} key is required");
            if (string.IsNullOrWhiteSpace(mission.Name)) errors.Add($"{label} '{mission.Key}' name is required");
            if (mission.BaseTarget <= 0) errors.Add($"{label} '{mission.Key}' base target must be positive");
        }
    }

    private static void ValidateShopItems(IReadOnlyList<GuildShopItemDefinition> items, List<string> errors)
    {
        if (items.Count == 0)
        {
            errors.Add("At least one guild shop item definition is required");
            return;
        }

        AddDuplicateErrors(items.Select(x => x.Key), "shop item key", errors);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Key)) errors.Add("Shop item key is required");
            if (string.IsNullOrWhiteSpace(item.Name)) errors.Add($"Shop item '{item.Key}' name is required");
            if (item.GuildFavorCost < 0 || item.GuildHonorsCost < 0) errors.Add($"Shop item '{item.Key}' costs cannot be negative");
            if (item.WeeklyLimit < 0) errors.Add($"Shop item '{item.Key}' weekly limit cannot be negative");
            if (item.RequiredWeeklyContribution < 0) errors.Add($"Shop item '{item.Key}' weekly contribution requirement cannot be negative");
            if (item.RequiredMarketOfficeLevel <= 0) errors.Add($"Shop item '{item.Key}' Market Office requirement must be positive");
            if (item.RotatesWeekly && string.IsNullOrWhiteSpace(item.RotationGroup)) errors.Add($"Shop item '{item.Key}' rotation group is required");
            if (item.Rewards.Count == 0) errors.Add($"Shop item '{item.Key}' must define at least one reward");

            foreach (var reward in item.Rewards)
            {
                if (reward.Amount <= 0) errors.Add($"Shop item '{item.Key}' reward amount must be positive");
                if ((reward.Type is GuildShopRewardType.Item or GuildShopRewardType.Title) && string.IsNullOrWhiteSpace(reward.Key))
                    errors.Add($"Shop item '{item.Key}' {reward.Type} reward requires a key");
            }
        }
    }

    private static void AddDuplicateErrors(IEnumerable<string> values, string label, List<string> errors)
    {
        foreach (var duplicate in values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key))
        {
            errors.Add($"Duplicate {label}: {duplicate}");
        }
    }
}

public sealed record GuildBuildingDefinition(
    GuildBuildingType Type,
    string Name,
    string Description,
    int MaxLevel,
    bool IsPermanent,
    int RequiredGuildHallLevel,
    string UnlockSummary,
    int BaseCost,
    int UpgradeCostStep,
    int BaseHours,
    IReadOnlyList<GuildBuildingBenefitDto> Benefits);

public sealed record GuildMissionDefinition(
    Guid Id,
    string Key,
    string Name,
    string Description,
    GuildMissionCategory Category,
    GuildContributionMetric Metric,
    long BaseTarget);

public sealed record GuildShopItemDefinition(
    string Key,
    string Name,
    string Description,
    GuildShopStockType StockType,
    long GuildFavorCost,
    long GuildHonorsCost,
    int WeeklyLimit,
    long RequiredWeeklyContribution,
    int RequiredMarketOfficeLevel,
    bool RotatesWeekly,
    string? RotationGroup,
    IReadOnlyList<GuildShopRewardDto> Rewards);

internal static class GuildContentHelpers
{
    public static IReadOnlyList<T> PickWeeklyRotation<T>(
        IEnumerable<T> items,
        string weekKey,
        int count,
        Func<T, string> keySelector)
    {
        var source = items.ToList();
        if (source.Count <= count) return source.OrderBy(keySelector).ToList();

        return source
            .OrderBy(item => StableHash($"{weekKey}:{keySelector(item)}"))
            .ThenBy(keySelector)
            .Take(count)
            .ToList();
    }

    public static int StableHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return BitConverter.ToInt32(bytes, 0) & int.MaxValue;
    }
}

internal static class GuildContentDefaults
{
    public static GuildContentDocument Document => new()
    {
        Buildings = BuildingDefinitions,
        WeeklyMissions = WeeklyMissionDefinitions,
        DailyOrders = DailyOrderDefinitions,
        ShopItems = ShopItemDefinitions
    };

    private static readonly GuildBuildingDefinition[] BuildingDefinitions =
    [
        new(
            GuildBuildingType.GuildHall,
            "Guild Hall",
            "The heart of the guild. Its level anchors the rest of the guild headquarters.",
            MaxLevel: 10,
            IsPermanent: true,
            RequiredGuildHallLevel: 1,
            "Higher levels unlock access to advanced guild buildings.",
            BaseCost: 150,
            UpgradeCostStep: 150,
            BaseHours: 4,
            Benefits:
            [
                new(1, "Guild Headquarters", "The guild can manage missions, supplies, and construction.", true),
                new(2, "Support Buildings", "Workshop and Treasury become available.", true),
                new(4, "Combat Infrastructure", "Raid Hall, Training Grounds, and Essence Sanctum become available.", true),
                new(6, "War Planning", "War Room becomes available.", true)
            ]),
        new(
            GuildBuildingType.MissionBoard,
            "Mission Board",
            "Improves weekly mission selection, daily orders, and guild mission rewards.",
            MaxLevel: 5,
            IsPermanent: false,
            RequiredGuildHallLevel: 1,
            "Improves guild mission variety and rewards.",
            BaseCost: 100,
            UpgradeCostStep: 100,
            BaseHours: 2,
            Benefits:
            [
                new(1, "Mission Infrastructure", "Unlocks mission reward bonuses and keeps the mission system active.", true),
                new(2, "Mission Variety", "Adds a fourth weekly mission option when enough definitions are available.", true),
                new(3, "Improved Orders", "Adds a fourth personal daily order when enough definitions are available.", true),
                new(5, "Premium Contracts", "Personal and weekly mission currency, XP, and supply rewards gain the full board bonus.", true)
            ]),
        new(
            GuildBuildingType.MarketOffice,
            "Market Office",
            "Unlocks and improves the guild shop.",
            MaxLevel: 5,
            IsPermanent: false,
            RequiredGuildHallLevel: 1,
            "Unlocks common, weekly, and prestige shop stock by level.",
            BaseCost: 150,
            UpgradeCostStep: 125,
            BaseHours: 2,
            Benefits:
            [
                new(1, "Common Stock", "Common guild shop stock can be purchased.", true),
                new(2, "Expanded Stock", "Additional common stock appears in the shop.", true),
                new(3, "Weekly Stock", "Rotating weekly shop stock becomes available.", true),
                new(5, "Prestige Procurement", "Prestige stock becomes available.", true)
            ]),
        new(
            GuildBuildingType.RaidHall,
            "Raid Hall",
            "Prepares the guild for cooperative raid content.",
            MaxLevel: 5,
            IsPermanent: false,
            RequiredGuildHallLevel: 4,
            "Unlock placeholder for future guild raids.",
            BaseCost: 400,
            UpgradeCostStep: 225,
            BaseHours: 6,
            Benefits:
            [
                new(1, "Raid Readiness", "Unlocks the Raids tab locked state and prepares future raid registration.", true),
                new(2, "Raid Registration", "Planned support for opening raid sign-ups.", false),
                new(3, "Raid Coordination", "Planned support for raid contribution scoring.", false),
                new(5, "Raid Spoils", "Planned support for improved raid rewards.", false)
            ]),
        new(
            GuildBuildingType.WarRoom,
            "War Room",
            "Prepares the guild for future war registration and war planning.",
            MaxLevel: 5,
            IsPermanent: false,
            RequiredGuildHallLevel: 6,
            "Unlock placeholder for future guild wars.",
            BaseCost: 500,
            UpgradeCostStep: 250,
            BaseHours: 8,
            Benefits:
            [
                new(1, "War Planning", "Unlocks the Wars tab locked state and prepares future war registration.", true),
                new(2, "Roster Planning", "Planned support for war roster setup.", false),
                new(3, "Defense Planning", "Planned support for defense preparation.", false),
                new(5, "Season Strategy", "Planned support for seasonal war progression.", false)
            ]),
        new(
            GuildBuildingType.Workshop,
            "Workshop",
            "Supports crafting-focused guild progression.",
            MaxLevel: 5,
            IsPermanent: false,
            RequiredGuildHallLevel: 2,
            "Adds crafting-flavored mission and shop support.",
            BaseCost: 175,
            UpgradeCostStep: 150,
            BaseHours: 3,
            Benefits:
            [
                new(1, "Craft Orders", "Crafting and tempering guild orders can appear from the data-driven order pool.", true),
                new(2, "Workshop Stock", "Workshop-themed shop stock can be configured through guild content data.", true),
                new(3, "Supply Efficiency", "Planned support for crafting-related Guild Supply bonuses.", false),
                new(5, "Masterwork Support", "Planned support for high-tier crafting objectives.", false)
            ]),
        new(
            GuildBuildingType.TrainingGrounds,
            "Training Grounds",
            "Supports future raid and war preparation.",
            MaxLevel: 5,
            IsPermanent: false,
            RequiredGuildHallLevel: 4,
            "Future raid and war support effects.",
            BaseCost: 300,
            UpgradeCostStep: 200,
            BaseHours: 5,
            Benefits:
            [
                new(1, "Training Yard", "Unlocks the building foundation for combat preparation systems.", true),
                new(2, "Raid Drills", "Planned support for raid preparation bonuses.", false),
                new(3, "War Drills", "Planned support for war preparation bonuses.", false),
                new(5, "Veteran Training", "Planned support for advanced combat guild objectives.", false)
            ]),
        new(
            GuildBuildingType.EssenceSanctum,
            "Essence Sanctum",
            "Supports essence-themed guild progression.",
            MaxLevel: 5,
            IsPermanent: false,
            RequiredGuildHallLevel: 4,
            "Future essence missions and rewards.",
            BaseCost: 300,
            UpgradeCostStep: 200,
            BaseHours: 5,
            Benefits:
            [
                new(1, "Essence Infrastructure", "Unlocks the building foundation for essence-focused guild systems.", true),
                new(2, "Essence Orders", "Planned support for essence guild objectives.", false),
                new(3, "Essence Rewards", "Planned support for essence-themed guild rewards.", false),
                new(5, "Sanctum Rituals", "Planned support for advanced essence progression.", false)
            ]),
        new(
            GuildBuildingType.Treasury,
            "Treasury",
            "Improves Guild Supply construction efficiency.",
            MaxLevel: 5,
            IsPermanent: false,
            RequiredGuildHallLevel: 2,
            "Reduces building Guild Supply costs and construction time.",
            BaseCost: 175,
            UpgradeCostStep: 150,
            BaseHours: 3,
            Benefits:
            [
                new(1, "Supply Ledger", "Treasury level reduces building Guild Supply costs by 2% per level.", true),
                new(2, "Supply Storage", "Guild Supply handling is represented through construction efficiency.", true),
                new(3, "Construction Efficiency", "Treasury level reduces building construction time by 2% per level.", true),
                new(5, "Quartermaster Network", "Treasury efficiency reaches its current maximum.", true)
            ])
    ];

    private static readonly GuildMissionDefinition[] WeeklyMissionDefinitions =
    [
        new(Guid.Parse("2f4bbec5-6212-47af-8b1d-e34cae632ae5"), "weekly.monster_extermination", "Monster Extermination", "Defeat creatures together as a guild.", GuildMissionCategory.Combat, GuildContributionMetric.CreaturesDefeated, 2000),
        new(Guid.Parse("00cf74dd-0b37-457b-a2b2-94a7630307e1"), "weekly.dungeon_expedition", "Dungeon Expedition", "Clear dungeon rooms together as a guild.", GuildMissionCategory.Dungeon, GuildContributionMetric.DungeonRoomsCleared, 50),
        new(Guid.Parse("d9a3f2d8-3a97-4e16-80fe-496d87afea70"), "weekly.craftsmens_commission", "Craftsmen's Commission", "Complete tempering actions together as a guild.", GuildMissionCategory.Crafting, GuildContributionMetric.TemperingActionsCompleted, 250),
        new(Guid.Parse("9b2b2643-2bd2-43ac-bb50-5a6d7b6e45c1"), "weekly.forge_quota", "Forge Quota", "Craft items together as a guild.", GuildMissionCategory.Crafting, GuildContributionMetric.ItemsCrafted, 75),
        new(Guid.Parse("683a9d85-8d9c-4750-b9c7-e89f8b2b9b61"), "weekly.dungeon_vanguard", "Dungeon Vanguard", "Complete dungeon runs together as a guild.", GuildMissionCategory.Dungeon, GuildContributionMetric.DungeonsCompleted, 12)
    ];

    private static readonly GuildMissionDefinition[] DailyOrderDefinitions =
    [
        new(Guid.Parse("8d7a12db-39eb-44f0-8c66-3ba79b606ca2"), "daily.creatures_defeated", "Cull the Wilds", "Defeat 100 creatures while in a guild.", GuildMissionCategory.Combat, GuildContributionMetric.CreaturesDefeated, 100),
        new(Guid.Parse("ff235b05-d721-4d96-b603-609c8abed319"), "daily.dungeon_rooms", "Scout the Depths", "Clear 5 dungeon rooms.", GuildMissionCategory.Dungeon, GuildContributionMetric.DungeonRoomsCleared, 5),
        new(Guid.Parse("c4ec6549-2bdc-4c7d-8494-6a5d8fbe7df2"), "daily.tempering_actions", "Temper the Arsenal", "Complete 20 tempering actions.", GuildMissionCategory.Crafting, GuildContributionMetric.TemperingActionsCompleted, 20),
        new(Guid.Parse("73254589-f56f-470b-9944-aa4d3a03322e"), "daily.items_crafted", "Stock the Workshop", "Craft 5 items.", GuildMissionCategory.Crafting, GuildContributionMetric.ItemsCrafted, 5)
    ];

    private static readonly GuildShopItemDefinition[] ShopItemDefinitions =
    [
        new("common.cinder_purse", "Cinder Purse", "A practical packet of Cinders for everyday upgrades.", GuildShopStockType.Common, 75, 0, 5, 0, 1, false, null, [new GuildShopRewardDto(GuildShopRewardType.Cinders, 500)]),
        new("common.soulstone_cache", "Soulstone Cache", "A small cache of Soulstones earned through guild service.", GuildShopStockType.Common, 125, 0, 3, 0, 1, false, null, [new GuildShopRewardDto(GuildShopRewardType.Soulstones, 2)]),
        new("common.cinder_satchel", "Cinder Satchel", "A larger packet made available through an expanded Market Office.", GuildShopStockType.Common, 180, 0, 2, 100, 2, false, null, [new GuildShopRewardDto(GuildShopRewardType.Cinders, 1200)]),
        new("weekly.builder_crate", "Builder's Crate", "A weekly support crate for active guild contributors.", GuildShopStockType.Weekly, 250, 0, 1, 250, 3, true, "weekly", [new GuildShopRewardDto(GuildShopRewardType.Cinders, 1500), new GuildShopRewardDto(GuildShopRewardType.Soulstones, 3)]),
        new("weekly.soulstone_bundle", "Soulstone Bundle", "A rotating bundle for members who kept the weekly ledger moving.", GuildShopStockType.Weekly, 300, 0, 1, 300, 3, true, "weekly", [new GuildShopRewardDto(GuildShopRewardType.Soulstones, 6)]),
        new("weekly.echo_stipend", "Echo Stipend", "A focused weekly stipend for prophecy and fate planning.", GuildShopStockType.Weekly, 350, 0, 1, 350, 3, true, "weekly", [new GuildShopRewardDto(GuildShopRewardType.FateEcho, 15), new GuildShopRewardDto(GuildShopRewardType.SigilFragments, 100)]),
        new("prestige.honor_reliquary", "Honor Reliquary", "A rare prestige reliquary reserved for proven guild champions.", GuildShopStockType.Prestige, 0, 2, 1, 500, 5, true, "prestige", [new GuildShopRewardDto(GuildShopRewardType.FateEcho, 25), new GuildShopRewardDto(GuildShopRewardType.Soulstones, 8), new GuildShopRewardDto(GuildShopRewardType.AscensionStoneFragments, 5)]),
        new("prestige.elder_cache", "Elder Cache", "A rotating prestige cache for members with exceptional weekly standing.", GuildShopStockType.Prestige, 0, 3, 1, 750, 5, true, "prestige", [new GuildShopRewardDto(GuildShopRewardType.Cinders, 3500), new GuildShopRewardDto(GuildShopRewardType.Soulstones, 10), new GuildShopRewardDto(GuildShopRewardType.SigilFragments, 250)])
    ];
}
