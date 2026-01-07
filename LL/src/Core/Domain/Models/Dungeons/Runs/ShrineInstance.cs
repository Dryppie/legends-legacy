namespace Domain.Models.Dungeons.Runs;

public sealed class ShrineInstance
{
    public List<Guid> OfferedBlessingIds { get; set; } = new();
    public Guid? SelectedBlessingId { get; set; }
    public bool Resolved { get; set; }
}
