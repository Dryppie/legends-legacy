namespace Domain.Models.Dungeons.Requirements;
public sealed class LevelRequirement : Requirement
{
    public int MinLevel { get; private set; }
    public LevelRequirement(int minLevel) { Discriminator = nameof(LevelRequirement); MinLevel = minLevel; }
    //public override bool IsSatisfiedBy(PlayerContext p) => p.Level >= MinLevel;
}
