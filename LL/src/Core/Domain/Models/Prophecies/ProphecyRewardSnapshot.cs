namespace Domain.Models.Prophecies;

public sealed class ProphecyRewardSnapshot
{
    public long Cinders { get; set; }
    public long CharacterExperience { get; set; }
    public long EssenceExperience { get; set; }
    public int Soulstones { get; set; }
    public int SigilFragments { get; set; }
    public int PropheticFavor { get; set; }
    public int FateEcho { get; set; }
    public string? CacheItemId { get; set; }
    public List<RewardItemSnapshot> Items { get; set; } = [];
}

public sealed class RewardItemSnapshot
{
    public string ItemId { get; set; } = default!;
    public int Quantity { get; set; }
}
