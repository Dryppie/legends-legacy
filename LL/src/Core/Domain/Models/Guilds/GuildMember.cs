using Domain.Models.Entities.Characters;

namespace Domain.Models.Guilds;
public class GuildMember
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public GuildRole Role { get; set; } = GuildRole.Member;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}