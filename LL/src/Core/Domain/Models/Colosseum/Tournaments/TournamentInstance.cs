namespace Domain.Models.Colosseum.Tournaments;

public sealed class TournamentInstance
{
    public Guid Id { get; set; }
    public Guid DefinitionId { get; set; }
    public TournamentDefinition Definition { get; set; } = null!;
    public int TournamentNumber { get; set; }
    public string Name { get; set; } = null!;
    public TournamentStatus Status { get; set; }
    public DateTimeOffset RegistrationStartsAtUtc { get; set; }
    public DateTimeOffset RegistrationEndsAtUtc { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public int MinParticipants { get; set; }
    public int MaxParticipants { get; set; }
    public int RoundIntervalMinutes { get; set; }
    public int RegisteredParticipantCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
