namespace Application.Interfaces.WebSockets;
public abstract record Audience
{
    public sealed record Character(Guid CharacterId) : Audience;
    public sealed record Characters(IReadOnlyCollection<Guid> CharacterIds) : Audience;
    public sealed record Guild(Guid GuildId) : Audience;
    public sealed record Raid(Guid RaidRunId) : Audience;
    public sealed record TournamentGrounds : Audience;
    public sealed record World : Audience;
}
