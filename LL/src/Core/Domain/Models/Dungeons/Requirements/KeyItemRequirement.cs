namespace Domain.Models.Dungeons.Requirements;
public sealed class KeyItemRequirement : Requirement
{
    public Guid ItemId { get; private set; }
    public string DisplayName { get; private set; }
    public int Needed { get; private set; } = 1;
    public KeyItemRequirement(Guid itemId, string displayName, int needed = 1)
    { Discriminator = nameof(KeyItemRequirement); ItemId = itemId; DisplayName = displayName; Needed = needed; }
    //public override bool IsSatisfiedBy(PlayerContext p) => p.InventoryCount(ItemId) >= Needed;
}
