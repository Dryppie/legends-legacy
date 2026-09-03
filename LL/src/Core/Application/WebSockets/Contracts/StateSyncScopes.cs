namespace Application.WebSockets.Contracts;

public static class StateSyncScopes
{
    public const string Character = "character";
    public const string CharacterOverview = "character-overview";
    public const string Inventory = "inventory";
    public const string LootHistory = "loot-history";
    public const string Equipment = "equipment";
    public const string EquipmentForge = "equipment-forge";
    public const string Quests = "quests";
    public const string AreaAccess = "area-access";
    public const string EventQuests = "event-quests";
    public const string Achievements = "achievements";
    public const string Essences = "essences";
    public const string Soulstones = "soulstones";
    public const string Dungeons = "dungeons";
    public const string Prophecies = "prophecies";
    public const string Marketplace = "marketplace";
    public const string Guild = "guild";
    public const string GuildBuildings = "guild-buildings";
    public const string GuildMissions = "guild-missions";
    public const string GuildShop = "guild-shop";
    public const string GuildMembership = "guild-membership";
    public const string GuildInvites = "guild-invites";
    public const string GuildDirectory = "guild-directory";
    public const string Colosseum = "colosseum";
    public const string Tournament = "tournament";
    public const string RaidDirectory = "raid-directory";
    public const string WorldTower = "world-tower";

    public static readonly IReadOnlyList<string> CharacterResources =
    [
        Character,
        CharacterOverview,
        Inventory,
        LootHistory,
        Equipment,
        EquipmentForge,
        Quests,
        AreaAccess,
        EventQuests,
        Achievements,
        Essences,
        Soulstones,
        Dungeons,
        Prophecies,
        Colosseum,
        GuildShop,
        GuildMembership,
        GuildInvites
    ];

    public static readonly IReadOnlyList<string> WorldResources =
    [
        Marketplace,
        GuildDirectory,
        Tournament,
        RaidDirectory,
        WorldTower
    ];

    public static readonly IReadOnlyList<string> GuildResources =
    [
        Guild,
        GuildBuildings,
        GuildMissions
    ];
}
