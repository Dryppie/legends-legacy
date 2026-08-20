namespace Domain.Models.Chats;

public sealed class RaidChatChannel
{
    public Guid RaidRunId { get; set; }
    public long Revision { get; set; }
    public bool IsOpen { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<RaidChatMembership> Memberships { get; set; } = [];
}

public sealed class RaidChatMembership
{
    public Guid RaidRunId { get; set; }
    public RaidChatChannel Channel { get; set; } = null!;
    public Guid CharacterId { get; set; }
}
