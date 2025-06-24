namespace Domain.Models.Guilds;
public class GuildResource
{
    public Guid GuildId { get; set; }
    public GuildResourceType Resource { get; set; } = default!;
    public int Amount { get; set; }
}
