namespace Services.LL.Colosseum.Tournaments;

public sealed class TournamentGroundsOptions
{
    public bool Enabled { get; set; } = true;
    public bool DevelopmentToolsEnabled { get; set; }
    public int DevelopmentProgressionIntervalSeconds { get; set; } = 2;
    public int ProgressionIntervalSeconds { get; set; } = 60;
    public bool UsePostgresAdvisoryLocks { get; set; } = true;
    public string DefaultDefinitionKey { get; set; } = "weekly-open-grounds";
    public string DefaultName { get; set; } = "Weekly Open Grounds";
    public string DefaultDescription { get; set; } = "Weekly live single-elimination arena tournament with one spectatable match every ten minutes.";
    public DayOfWeek DefaultRegistrationStartDayUtc { get; set; } = DayOfWeek.Monday;
    public int DefaultRegistrationStartHourUtc { get; set; } = 0;
    public DayOfWeek DefaultRegistrationEndDayUtc { get; set; } = DayOfWeek.Saturday;
    public int DefaultRegistrationEndHourUtc { get; set; } = 0;
    public int DefaultStartDelayAfterRegistrationMinutes { get; set; } = 0;
    public int DefaultRoundIntervalMinutes { get; set; } = 10;
    public int MatchIntervalMinutes { get; set; } = 10;
    public int PlaybackCompletionGraceSeconds { get; set; } = 1;
    public int CombatTicksPerFrame { get; set; } = 10;
    public int MaximumBundleUncompressedBytes { get; set; } = 16 * 1024 * 1024;
    public int MaximumBundleCompressedBytes { get; set; } = 4 * 1024 * 1024;
    public int DefaultMinParticipants { get; set; } = 4;
    public int DefaultMaxParticipants { get; set; } = 32;
    public int? DefaultMinimumCharacterLevel { get; set; } = 1;
    public int? DefaultMinimumArenaRating { get; set; }
    public string? DefaultMinimumRankTier { get; set; }
    public bool AllowWithdrawDuringRegistration { get; set; } = true;
    public bool RequireValidArenaDefenseSnapshot { get; set; }
    public List<TournamentRewardTierOptions> Rewards { get; set; } = [];
}

public sealed class TournamentRewardTierOptions
{
    public string Key { get; set; } = "participant";
    public int? MaxPlacement { get; set; }
    public int ArenaGlory { get; set; }
    public int Cinders { get; set; }
    public int Soulstones { get; set; }
}
