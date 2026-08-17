using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Guilds;
using Application.UseCases.Outbox;

namespace Services.LL.Guilds;

public sealed class GuildSystemChatPublisher(
    ICharacterService characters,
    IGameEventOutbox outbox) : IGuildSystemChatPublisher
{
    public async Task PublishAsync(
        Guid guildId,
        Guid subjectCharacterId,
        GuildSystemChatEvent eventType,
        CancellationToken cancellationToken)
    {
        await PublishBodyAsync(
            guildId,
            subjectCharacterId,
            BodyFor(eventType),
            cancellationToken);
    }

    public async Task PublishBuildingAsync(
        Guid guildId,
        Guid actorCharacterId,
        string buildingName,
        int buildingLevel,
        GuildBuildingChatEvent eventType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(buildingName) || buildingLevel < 1)
        {
            throw new ArgumentException("Guild building chat details are invalid.");
        }

        var action = eventType switch
        {
            GuildBuildingChatEvent.Constructed => "built",
            GuildBuildingChatEvent.Upgraded => "upgraded",
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null)
        };
        await PublishBodyAsync(
            guildId,
            actorCharacterId,
            $"{action} {buildingName.Trim()} to level {buildingLevel}.",
            cancellationToken);
    }

    private async Task PublishBodyAsync(
        Guid guildId,
        Guid subjectCharacterId,
        string body,
        CancellationToken cancellationToken)
    {
        var subject = await characters.GetBaseCharacterByIdAsync(
            subjectCharacterId,
            cancellationToken);
        if (subject is null)
        {
            throw new InvalidOperationException(
                $"Guild chat subject {subjectCharacterId} was not found.");
        }

        await outbox.EnqueueAsync(
            GameEventTypes.GuildChatMessage,
            new GuildChatMessagePayload(
                guildId,
                subject.Id,
                subject.Name,
                body,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow),
            subject.Id,
            subject.UserId,
            cancellationToken);
    }

    private static string BodyFor(GuildSystemChatEvent eventType) => eventType switch
    {
        GuildSystemChatEvent.Joined => "joined the guild.",
        GuildSystemChatEvent.Kicked => "was kicked from the guild.",
        GuildSystemChatEvent.Left => "left the guild.",
        GuildSystemChatEvent.PromotedToOfficer => "was promoted to Officer.",
        GuildSystemChatEvent.DemotedToMember => "was demoted to Member.",
        GuildSystemChatEvent.Invited => "was invited to the guild.",
        _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null)
    };
}
