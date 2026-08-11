namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed class IdleCombatProgressionOptions
{
    public const string SectionName = "Combat:IdleProgression";

    public int EncounterCadenceSeconds { get; set; } = 10;
    public int MaximumOfflineHours { get; set; } = 24;
    public int MaximumEncountersPerResolution { get; set; } = 100;
    public int ReferenceWinRateBasisPoints { get; set; } = 8_500;
}
