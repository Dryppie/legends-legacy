using Application.Interfaces.Services.LL.Tutorials;

namespace Application.WebSockets.Contracts;
public abstract record GameEventMsg;

public record SaleCompletedMsg(Guid ItemId, Guid SellerId, int Price) : GameEventMsg;
public record GuildApplicationMsg(Guid GuildId, Guid PlayerId) : GameEventMsg;
public record GuildInviteReceivedMsg(Guid GuildId, Guid CharacterId) : GameEventMsg;
public record GuildInviteRejectedMsg(Guid GuildId, Guid CharacterId) : GameEventMsg;
public record GuildApplicationRejectedMsg(Guid GuildId, Guid CharacterId) : GameEventMsg;
public record GuildBuildingsChangedMsg(Guid GuildId, string BuildingId) : GameEventMsg;
public record GuildStateChangedMsg(Guid GuildId) : GameEventMsg;
public record GuildMembershipChangedMsg(Guid GuildId, Guid CharacterId) : GameEventMsg;
public record GuildDisbandedMsg(Guid GuildId) : GameEventMsg;
public record GuildDirectoryChangedMsg(string Reason) : GameEventMsg;
public record RiftOpenedMsg(Guid ZoneId, DateTimeOffset Time) : GameEventMsg;
public record AchievementUnlockedMsg(
    Guid? CharacterId,
    string AchievementKey,
    string AchievementName,
    int Points,
    string? TitleKey,
    string? TitleName,
    string Message,
    bool IsGlobal) : GameEventMsg;

public record TutorialProgressedMsg(TutorialState Tutorial) : GameEventMsg;

public record TutorialCompletedMsg(string TutorialId) : GameEventMsg;
//public record LootReceivedMsg(Guid CharacterId, List<InventoryItemDto> Items) : GameEventMsg;
