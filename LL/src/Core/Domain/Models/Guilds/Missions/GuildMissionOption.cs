namespace Domain.Models.Guilds.Missions;

public class GuildMissionOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Guid MissionDefinitionId { get; set; }
    public string WeekKey { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsSelected { get; set; }
    public DateTimeOffset? SelectedAt { get; set; }
    public Guid? SelectedByCharacterId { get; set; }
}
