namespace Application.Interfaces.Services.LL.CombatStyles;

public interface ICombatStyleBalanceSimulator
{
    CombatStyleBalanceSimulationReport Run(CombatStyleBalanceSimulationRequest request);
}

public sealed record CombatStyleBalanceSimulationRequest(
    int BattleCount,
    int StyleLevel,
    int RandomSeed,
    int TopResults,
    bool IncludeFocuses);

public sealed record CombatStyleBalanceSimulationReport(
    int BattleCount,
    int BattlesRun,
    int StyleLevel,
    int RandomSeed,
    int CandidateCount,
    IReadOnlyList<CombatStyleBalanceResult> RankedStyles,
    IReadOnlyList<CombatStyleBalanceBattleSummary> BattleSummaries);

public sealed record CombatStyleBalanceResult(
    string StyleId,
    string StyleName,
    string? FocusId,
    string? FocusName,
    int Battles,
    int Wins,
    int Losses,
    int Draws,
    double WinRate,
    double AverageDuration,
    double AverageDamageDone,
    double AverageDamageTaken);

public sealed record CombatStyleBalanceBattleSummary(
    int Index,
    string FriendlyStyleId,
    string FriendlyStyleName,
    string? FriendlyFocusId,
    string HostileStyleId,
    string HostileStyleName,
    string? HostileFocusId,
    string Outcome,
    int Duration,
    int FriendlyDamageDone,
    int FriendlyDamageTaken,
    int HostileDamageDone,
    int HostileDamageTaken);
