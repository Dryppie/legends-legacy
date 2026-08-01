namespace Domain.Models.Guilds.Buildings;

public class GuildBuilding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public GuildBuildingType Type { get; set; }
    public int Level { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
