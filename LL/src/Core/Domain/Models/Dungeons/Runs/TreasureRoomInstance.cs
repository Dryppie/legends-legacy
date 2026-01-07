namespace Domain.Models.Dungeons.Runs;

public sealed class TreasureRoomInstance
{
    public List<TreasureOptionInstance> Options { get; set; } = new();
    public bool Resolved { get; set; }
    public int? SelectedOptionIndex { get; set; }
}
