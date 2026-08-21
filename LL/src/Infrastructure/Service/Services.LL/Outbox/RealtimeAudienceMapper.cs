using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;

namespace Services.LL.Outbox;

internal static class RealtimeAudienceMapper
{
    public static RealtimeAudiencePayload ToPayload(Audience audience) => audience switch
    {
        Audience.Character character => new("character", character.CharacterId, null),
        Audience.Characters characters => new(
            "characters",
            null,
            characters.CharacterIds.Distinct().ToArray()),
        Audience.Guild guild => new("guild", guild.GuildId, null),
        Audience.Raid raid => new("raid", raid.RaidRunId, null),
        Audience.TournamentGrounds => new("tournament-grounds", null, null),
        Audience.World => new("world", null, null),
        _ => throw new ArgumentException(
            $"Unsupported audience type: {audience.GetType().Name}",
            nameof(audience))
    };

    public static Guid? CharacterId(Audience audience) =>
        audience is Audience.Character character ? character.CharacterId : null;
}
