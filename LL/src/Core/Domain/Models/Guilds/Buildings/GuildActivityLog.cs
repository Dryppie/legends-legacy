namespace Domain.Models.Guilds.Buildings;

public class GuildActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public GuildActivityLogType Type { get; set; }
    public Guid? CharacterId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
