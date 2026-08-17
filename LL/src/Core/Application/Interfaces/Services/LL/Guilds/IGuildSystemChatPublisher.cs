namespace Application.Interfaces.Services.LL.Guilds;

public enum GuildSystemChatEvent
{
    Joined,
    Kicked,
    Left,
    PromotedToOfficer,
    DemotedToMember,
    Invited
}

public enum GuildBuildingChatEvent
{
    Constructed,
    Upgraded
}

public interface IGuildSystemChatPublisher
{
    Task PublishAsync(
        Guid guildId,
        Guid subjectCharacterId,
        GuildSystemChatEvent eventType,
        CancellationToken cancellationToken);

    Task PublishBuildingAsync(
        Guid guildId,
        Guid actorCharacterId,
        string buildingName,
        int buildingLevel,
        GuildBuildingChatEvent eventType,
        CancellationToken cancellationToken);
}
