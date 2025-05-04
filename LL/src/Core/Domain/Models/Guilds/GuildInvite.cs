using Domain.Models.Entities.Characters;

namespace Domain.Models.Guilds;
public class GuildInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;

    /// <summary>
    /// If true, it's an invite that has been sent to a player by the guild
    /// If false, it's an application that has been sent to the guild by the player
    /// </summary>
    public bool IsInvite { get; set; }
}