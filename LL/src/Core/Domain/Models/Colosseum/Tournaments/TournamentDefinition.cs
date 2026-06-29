namespace Domain.Models.Colosseum.Tournaments;

public sealed class TournamentDefinition
{
    public Guid Id { get; set; }
    public string Key { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public TournamentFormat Format { get; set; }
    public int MinParticipants { get; set; }
    public int MaxParticipants { get; set; }
    public int RegistrationDurationMinutes { get; set; }
    public int StartDelayAfterRegistrationMinutes { get; set; }
    public int RoundIntervalMinutes { get; set; }
    public int? MinimumCharacterLevel { get; set; }
    public int? MinimumArenaRating { get; set; }
    public string? MinimumRankTier { get; set; }
    public bool Enabled { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
