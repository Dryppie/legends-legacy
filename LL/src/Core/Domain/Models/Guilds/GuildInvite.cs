using Domain.Models.Entities.Characters;

namespace Domain.Models.Guilds;
public class GuildInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
}