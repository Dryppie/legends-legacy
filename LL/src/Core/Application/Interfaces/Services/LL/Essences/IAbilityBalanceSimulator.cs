namespace Application.Interfaces.Services.LL.Essences;

public interface IAbilityBalanceSimulator
{
    AbilityBalanceSimulationReport Run(AbilityBalanceSimulationRequest request);
}

public sealed record AbilityBalanceSimulationRequest(
    int BattleCount,
    int TeamSize,
    int EssencesPerParticipant,
    int RandomSeed,
    int TopResults,
    int CandidatePoolSize,
    IReadOnlyList<AbilityBalanceTeamLoadout>? CandidateTeams);

public sealed record AbilityBalanceTeamLoadout(
    IReadOnlyList<AbilityBalanceParticipantLoadout> Participants);

public sealed record AbilityBalanceParticipantLoadout(
    IReadOnlyList<string> EssenceIds);

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
    IReadOnlyList<AbilityBalanceCombinationResult> RankedCombinations,
    IReadOnlyList<AbilityBalanceBattleSummary> BattleSummaries);

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
    double AverageDamageTaken);

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
