namespace Application.Interfaces.Services.LL.Essences;

public interface IAbilityBalanceAuditService
{
    AbilityBalanceAuditReport Run(
        AbilityBalanceAuditRequest request,
        CancellationToken cancellationToken);
}

public sealed record AbilityBalanceAuditRequest(
    int TeamSize = 3,
    int EssencesPerParticipant = 5,
    int CandidatePoolSize = 1_000,
    int ScreeningBattleCount = 25_000,
    int FinalistCount = 100,
    int FinalistBattleCount = 10,
    int ValidationBattleCount = 200,
    IReadOnlyList<int>? RandomSeeds = null,
    int EquipmentTier = 10,
    string EquipmentRarity = "Epic",
    string EquipmentProfile = "Balanced");

public sealed record AbilityBalanceAuditReport(
    string ContentHash,
    long ScreeningBattlesRun,
    long ValidationBattlesRun,
    long FinalistBattlesRun,
    long TotalBattlesRun,
    int CandidateTeamsTested,
    int FinalistTeamCount,
    int EquipmentTier,
    string EquipmentRarity,
    string EquipmentProfile,
    IReadOnlyDictionary<string, float> ParticipantAttributes,
    IReadOnlyList<AbilityBalanceEssenceResult> EssenceResults,
    IReadOnlyList<AbilityBalanceEssenceResult> FinalistEssenceResults,
    IReadOnlyList<AbilityBalanceValidationResult> ValidationResults,
    IReadOnlyList<AbilityBalanceCombinationResult> Finalists);

public sealed record AbilityBalanceValidationResult(
    string EssenceId,
    string DisplayName,
    string ReplacementEssenceId,
    string ReplacementDisplayName,
    int Battles,
    double OriginalScore,
    double ReplacementScore,
    double ScoreDelta,
    int ContextCount = 1,
    int ReplacementCount = 1);
