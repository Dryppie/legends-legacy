namespace Services.LL.CharacterActions;

public sealed class TemperingProgressionOptions
{
    public const string SectionName = "Crafting:TemperingProgression";

    /// <summary>Maximum attempts processed by one internal crafting batch.</summary>
    public int MaximumAttemptsPerResolution { get; set; } = 100;
    /// <summary>Maximum internal batches aggregated into one API resolution.</summary>
    public int MaximumBatchesPerResolution { get; set; } = 100;
}
