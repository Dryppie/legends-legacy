namespace Domain.Models.Dungeons.Runs;

public sealed class RunFlags
{
    public bool PassedCheckpoint { get; set; }
    public bool OpenedCursedChest { get; set; }

    // Limit course-correction to one essence swap at checkpoint.
    public bool UsedCheckpointEssenceSwap { get; set; }
}
