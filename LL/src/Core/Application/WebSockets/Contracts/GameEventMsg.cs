using Application.Interfaces.Services.LL.Quests;
using Application.UseCases.Equipments.Dtos;

namespace Application.WebSockets.Contracts;

public abstract record GameEventMsg;

public record SaleCompletedMsg(Guid ItemId, Guid SellerId, int Price) : GameEventMsg;
public record GuildApplicationMsg(Guid GuildId, Guid PlayerId) : GameEventMsg;
public record GuildInviteReceivedMsg(Guid GuildId, Guid CharacterId) : GameEventMsg;
public record GuildInviteRejectedMsg(Guid GuildId, Guid CharacterId) : GameEventMsg;
public record GuildApplicationRejectedMsg(Guid GuildId, Guid CharacterId) : GameEventMsg;
public record GuildBuildingsChangedMsg(Guid GuildId, string BuildingId) : GameEventMsg;
public record GuildStateChangedMsg(Guid GuildId) : GameEventMsg;
public record GuildVaultChatMessageMsg(
    Guid GuildId,
    Guid MessageId,
    Guid ActorCharacterId,
    string ActorName,
    string Action,
    EquipmentInstanceDto Equipment,
    DateTimeOffset SentAt) : GameEventMsg;
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

public record PlayerTransferMsg(
    Guid TransferId,
    Guid MessageId,
    Guid CharacterId,
    string Message) : GameEventMsg;

public record QuestJournalChangedMsg(QuestJournal Journal) : GameEventMsg;
public record EventQuestChangedMsg(string EventQuestId, DateTimeOffset UpdatedAt) : GameEventMsg;
