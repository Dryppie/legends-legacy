namespace Application.Interfaces.Services.LL.Essences;

public interface IAbilityBalanceSimulator
{
    AbilityBalanceSimulationReport Run(
        AbilityBalanceSimulationRequest request,
        CancellationToken cancellationToken = default,
        Action<AbilityBalanceSimulationProgress>? progress = null);
}

public sealed record AbilityBalanceSimulationProgress(long BattlesCompleted, long TotalBattles);

public sealed record AbilityBalanceSimulationRequest(
    int BattleCount,
    int TeamSize,
    int EssencesPerParticipant,
    int RandomSeed,
    int TopResults,
    int CandidatePoolSize,
    IReadOnlyList<AbilityBalanceTeamLoadout>? CandidateTeams,
    int EquipmentTier = 10,
    string EquipmentRarity = "Epic",
    string EquipmentProfile = "Balanced",
    bool UseCanonicalRoles = false);

public sealed record AbilityBalanceTeamLoadout(
    IReadOnlyList<AbilityBalanceParticipantLoadout> Participants);

public sealed record AbilityBalanceParticipantLoadout(
    IReadOnlyList<string> EssenceIds,
    string Role = "Balance");

public sealed record AbilityBalanceSimulationReport(
    string Mode,
    int RequestedBattleCount,
    int BattlesRun,
    int TeamSize,
    int EssencesPerParticipant,
    int RandomSeed,
    int CandidateTeamCount,
    int CandidatePoolSize,
    int AvailableEssenceCount,
    int EquipmentTier,
    string EquipmentRarity,
    string EquipmentProfile,
    IReadOnlyDictionary<string, float> ParticipantAttributes,
    IReadOnlyList<AbilityBalanceEssenceDefinition> AvailableEssences,
    IReadOnlyList<AbilityBalanceCombinationResult> RankedCombinations,
    IReadOnlyList<AbilityBalanceEssenceResult> EssenceResults,
    IReadOnlyList<AbilityBalanceBattleSummary> BattleSummaries,
    IReadOnlyList<AbilityBalanceMatchupResult>? MatchupResults = null,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, float>>? ParticipantAttributesByRole = null);

public sealed record AbilityBalanceEssenceDefinition(
    string EssenceId,
    string SourceMonsterId,
    IReadOnlyList<string> AbilityIds);

public sealed record AbilityBalanceCombinationResult(
    string Signature,
    string DisplayName,
    IReadOnlyList<AbilityBalanceParticipantLoadout> Participants,
    int Battles,
    int Wins,
    int Losses,
    int Draws,
    double WinRate,
    double LossRate,
    double DrawRate,
    double AverageDuration,
    double AverageDamageDone,
    double AverageDamageTaken,
    IReadOnlyList<AbilityBalanceSeedResult>? SeedResults = null);

public sealed record AbilityBalanceSeedResult(
    int RandomSeed,
    int Battles,
    double Score);

public sealed record AbilityBalanceMatchupResult(
    string FirstSignature,
    string SecondSignature,
    int Battles,
    int FirstWins,
    int SecondWins,
    int Draws,
    double FirstScore);

public sealed record AbilityBalanceEssenceResult(
    string EssenceId,
    string DisplayName,
    int TeamAppearances,
    int Battles,
    int Wins,
    int Losses,
    int Draws,
    double Score,
    double ScoreDelta,
    double AdjustedScoreDelta,
    double ConfidenceLower,
    double ConfidenceUpper,
    double AverageDuration,
    double AverageDamageDone,
    double AverageDamageTaken,
    string Classification);

public sealed record AbilityBalanceBattleSummary(
    int Index,
    string FriendlySignature,
    string FriendlyDisplayName,
    string HostileSignature,
    string HostileDisplayName,
    string Outcome,
    int Duration,
    int FriendlyDamageDone,
    int FriendlyDamageTaken,
    int HostileDamageDone,
    int HostileDamageTaken);
