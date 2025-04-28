using Domain.Models.Entities.Characters;

namespace Domain.Models.Guilds;
public class Guild
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxMembers { get; set; } = 10;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid OwnerCharacterId { get; set; }
    public Character OwnerCharacter { get; set; } = null!;
    public ICollection<GuildMember> Members { get; set; } = [];
    public ICollection<GuildInvite> Invites { get; set; } = [];
}