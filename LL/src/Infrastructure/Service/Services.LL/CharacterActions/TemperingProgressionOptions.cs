namespace Services.LL.CharacterActions;

public sealed class TemperingProgressionOptions
{
    public const string SectionName = "Crafting:TemperingProgression";

    public int MaximumAttemptsPerResolution { get; set; } = 100;
}
