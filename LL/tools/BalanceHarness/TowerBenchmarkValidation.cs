using System.Numerics;
using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerBenchmarkValidationFreeze(string Version, string PlanHash, string NominationPanelHash,
    string ChallengerId, string BenchmarkId);
public sealed record TowerBenchmarkValidationDecision(string Version, string FreezeHash, string PanelHash,
    int Samples, int GainedWins, int LostWins, long TailNumerator, long TailDenominator,
    bool Passed, string SelectedId);

/// <summary>One challenger frozen before one complete fresh paired panel. This
/// provisional search output gate is neither team confirmation nor adoption.</summary>
public static class TowerBenchmarkValidation
{
    public const string Version = "tower-racing-benchmark-validation-v1";
    public const int NominationSamples = 16;
    public const int ValidationSamples = 60;
    public const string NominationRole = "nomination";
    public const string ValidationRole = "validation";
    internal static IReadOnlyList<string> PanelRoles { get; } = Array.AsReadOnly(new[] {
        "wave-1-screen", "wave-1-continuation", "wave-2-screen", "wave-2-continuation", NominationRole, ValidationRole });

    internal static string SelectChallenger(IReadOnlyList<TowerPanelScore> scores, IReadOnlyList<string> nominees,
        string primaryId, string benchmarkId, IReadOnlySet<string>? excludedReferences = null)
    {
        if (nominees.Count != 5 || nominees.Distinct(StringComparer.Ordinal).Count() != 5
            || !nominees.Contains(benchmarkId) || !nominees.Contains(primaryId)
            || scores.Count != 5 || !scores.Select(s => s.Id).ToHashSet(StringComparer.Ordinal).SetEquals(nominees)
            || scores.Any(s => s.Samples != NominationSamples))
            throw new InvalidDataException("Validation requires five scored nominees including the bound references.");
        if (excludedReferences is not null)
        {
            if (excludedReferences.Count != 3 || !excludedReferences.Contains(benchmarkId)
                || !excludedReferences.Contains(primaryId) || excludedReferences.Any(id => !nominees.Contains(id)))
                throw new InvalidDataException("Generated-only nomination requires the three bound references.");
            return TowerBatchRacing.Select(scores.Where(s => !excludedReferences.Contains(s.Id)).ToArray(),
                nominees.Where(id => !excludedReferences.Contains(id)).ToArray(), primaryId);
        }
        // Keep the existing primary tie and zero-win health rules among the four
        // non-benchmark choices, even when the benchmark led the nomination panel.
        return TowerBatchRacing.Select(scores.Where(s => s.Id != benchmarkId).ToArray(),
            nominees.Where(id => id != benchmarkId).ToArray(), primaryId);
    }

    // Counts fit in Int64 for at most 60 discordant pairs. BigInteger also makes
    // the recurrence and alpha=1/20 comparison exact, without floating rounding.
    internal static (long Numerator, long Denominator, bool Passed) Gate(int gains, int losses)
    {
        if (gains is < 0 or > ValidationSamples || losses is < 0 or > ValidationSamples
            || gains + losses > ValidationSamples)
            throw new InvalidDataException("Invalid fixed-panel discordant-pair counts.");
        var discordant = gains + losses;
        BigInteger choose = 1, tail = 0;
        for (var k = 0; k <= discordant; k++)
        {
            if (k >= gains) tail += choose;
            if (k < discordant) choose = choose * (discordant - k) / (k + 1);
        }
        var denominator = BigInteger.One << discordant;
        return ((long)tail, (long)denominator, gains > losses && 20 * tail <= denominator);
    }

    internal static TowerBenchmarkValidationDecision Decide(TowerBenchmarkValidationFreeze frozen, TowerPanelEvaluation panel)
    {
        var f = panel.Freeze;
        if (frozen.Version != Version || !TowerProposalPolicies.UsesBenchmarkValidation(f.Version)
            || frozen.PlanHash != f.PlanHash || frozen.ChallengerId == frozen.BenchmarkId
            || !panel.Complete || f.Index != 5 || f.Role != ValidationRole || f.EvaluationsBefore != 408
            || f.PlannedEvaluations != 120 || f.Seeds.Count != ValidationSamples
            || f.Seeds.Distinct().Count() != ValidationSamples
            || !f.Parties.Select(p => p.Id).SequenceEqual(new[] { frozen.ChallengerId, frozen.BenchmarkId })
            || panel.Observations.Count != 120)
            throw new InvalidDataException("Only the complete frozen challenger/benchmark panel can decide an output.");
        var hash = HarnessJson.Hash(f);
        for (var index = 0; index < panel.Observations.Count; index++)
        {
            var row = panel.Observations[index];
            if (row.Request.PanelHash != hash || row.Request.ScopeHash != f.ScopeHash || row.Request.Role != ValidationRole
                || row.Request.Ordinal != f.EvaluationsBefore + index + 1
                || row.Request.PartyId != f.Parties[index / ValidationSamples].Id
                || row.Request.Seed != f.Seeds[index % ValidationSamples] || row.Outcome.Seed != row.Request.Seed
                || row.Outcome.RequestHash != HarnessJson.Hash(row.Request) || !Enum.IsDefined(row.Outcome.Outcome))
                throw new InvalidDataException("Validation outcomes must be complete and paired in frozen request order.");
        }
        var gains = 0; var losses = 0;
        for (var i = 0; i < ValidationSamples; i++)
        {
            var challenger = panel.Observations[i].Outcome.Outcome == BattleOutcome.Victory;
            var benchmark = panel.Observations[i + ValidationSamples].Outcome.Outcome == BattleOutcome.Victory;
            if (challenger && !benchmark) gains++;
            if (!challenger && benchmark) losses++;
        }
        var gate = Gate(gains, losses);
        return new(Version, HarnessJson.Hash(frozen), hash, ValidationSamples, gains, losses, gate.Numerator,
            gate.Denominator, gate.Passed, gate.Passed ? frozen.ChallengerId : frozen.BenchmarkId);
    }
}
