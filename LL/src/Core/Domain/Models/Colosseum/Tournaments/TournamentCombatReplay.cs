namespace Domain.Models.Colosseum.Tournaments;

public sealed class TournamentCombatReplay
{
    public const int MinimumCompactBundleSchemaVersion = 2;
    public const int CompactBundleSchemaVersion = 3;

    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public TournamentInstance Tournament { get; set; } = null!;
    public Guid MatchId { get; set; }
    public TournamentMatch Match { get; set; } = null!;
    public Guid CombatSessionId { get; set; }
    public Guid BattleHistoryId { get; set; }
    public Guid PlayerOneCharacterId { get; set; }
    public Guid PlayerTwoCharacterId { get; set; }
    public string Outcome { get; set; } = null!;
    public DateTimeOffset StartedAtUtc { get; set; }
    public int Duration { get; set; }
    public string? CombatResultJson { get; set; }
    public int SchemaVersion { get; set; } = 1;
    public int TicksPerSecond { get; set; }
    public int TicksPerFrame { get; set; }
    public int FrameCount { get; set; }
    public string? BundleHash { get; set; }
    public int? BundleLength { get; set; }
    public string? BundleContentType { get; set; }
    public string? BundleContentEncoding { get; set; }
    public TournamentCombatReplayArtifact? Artifact { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class TournamentCombatReplayArtifact
{
    public Guid TournamentCombatReplayId { get; set; }
    public TournamentCombatReplay Replay { get; set; } = null!;
    public byte[] BundleBytes { get; set; } = null!;
}
