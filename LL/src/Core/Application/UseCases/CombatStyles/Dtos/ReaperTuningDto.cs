namespace Application.UseCases.CombatStyles.Dtos;

public sealed record ReaperTuningDto
{
    public double BaseMultiplier { get; init; }
    public double PerMasteryLevel { get; init; }
    public double DeathSentenceBonus { get; init; }
    public double LastRitesHealthThreshold { get; init; }
    public double UpgradeBonus { get; init; }
    public double ClosingHandHealthThreshold { get; init; }
    public double MasteredClosingHandHealthThreshold { get; init; }
    public int OpeningPoisonStacks { get; init; }
}
