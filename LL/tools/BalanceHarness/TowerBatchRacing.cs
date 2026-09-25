using Domain.Models.Combat;

namespace BalanceHarness;

/// <summary>Fixed two-wave allocation over supplied teams. All scores are training
/// measurements; RawSelectedId is provisional and conveys no confirmation or adoption.</summary>
public static partial class TowerBatchRacing
{
    public delegate Task<TowerPanelOutcome> Evaluator(TowerPanelTrial request, CancellationToken token);

    public static async Task<TowerBatchRacingReport> RunAsync(TowerBatchRacingPlan plan, Evaluator evaluate,
        CancellationToken token = default, Action<TowerBatchRacingReport>? checkpoint = null)
    {
        ArgumentNullException.ThrowIfNull(evaluate);
        plan = Copy(plan);
        Validate(plan); // No evaluator or checkpoint is entered for an invalid plan.
        return await RunCoreAsync(Version, HarnessJson.Hash(plan), plan.Scope, plan.Panels, plan.MaximumEvaluations,
            (wave, _, _) => wave == 1 ? plan.FirstWave : plan.SecondWave, evaluate, token, checkpoint);
    }

    // Versions share the first two racing waves. Generated batches enter through
    // the callback; the frozen v1 wrapper validates both supplied batches up front.
    internal static async Task<TowerBatchRacingReport> RunCoreAsync(string version, string planHash,
        TowerBossDiscoveryDefinition d, IReadOnlyList<TowerRacingPanel> schedule, int maximum,
        Func<int, IReadOnlyList<PartyChoice>, IReadOnlyList<TowerPanelEvaluation>, IReadOnlyList<PartyChoice>> generate,
        Evaluator evaluate, CancellationToken token, Action<TowerBatchRacingReport>? checkpoint, bool panelFreezesOnly = false,
        string? positiveTieBenchmarkId = null, string? validationBenchmarkId = null)
    {
        if (positiveTieBenchmarkId is not null && validationBenchmarkId is not null)
            throw new InvalidDataException("Final output policies cannot be combined.");
        var scopeHash = HarnessJson.Hash(d);
        var references = d.Starts.Select(s => s.Party).ToArray();
        var referenceIds = references.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        var primaryId = d.Starts.Single(s => s.ReferenceId == d.Stages.SelectionPrimaryReferenceId).Party.Id;
        var context = d.Contexts.Single().Id;
        var panels = new List<TowerPanelEvaluation>();
        var decisions = new List<TowerRacingDecision>();
        var trials = new HashSet<string>(StringComparer.Ordinal);
        var seen = new List<PartyChoice>(references);
        var charged = 0;
        string[] nominees = [];
        string? selected = null;
        TowerBenchmarkValidationFreeze? validationFreeze = null;
        TowerBenchmarkValidationDecision? validationDecision = null;

        TowerBatchRacingReport Snapshot(string status, string? error = null) => Copy(new TowerBatchRacingReport(
            version, planHash, status, PlannedEvaluations, charged, panels, decisions, nominees, selected, error,
            validationFreeze, validationDecision));
        void Checkpoint(bool frozen = false)
        {
            if (!panelFreezesOnly || frozen) checkpoint?.Invoke(Snapshot("Running"));
        }

        async Task<TowerPanelEvaluation> Measure(IReadOnlyList<PartyChoice> parties)
        {
            token.ThrowIfCancellationRequested();
            var index = panels.Count;
            var panel = schedule[index];
            var cost = checked(parties.Count * panel.Seeds.Count);
            if (charged + cost > maximum || charged + cost > PlannedEvaluations)
                throw new InvalidDataException("Complete panel would exceed the fixed evaluation budget.");
            var freeze = new TowerPanelFreeze(version, planHash, scopeHash, index, panel.Role, context,
                panel.Seeds, parties, charged, cost);
            var panelHash = HarnessJson.Hash(freeze);
            var observations = new List<TowerPanelObservation>();
            panels.Add(new(freeze, false, observations, [], []));
            Checkpoint(true); // Entire membership, recipe and seed order precede the first request.
            foreach (var party in parties)
            {
                var scenario = TowerBossDiscovery.Scenario(d, context, party, panel.Seeds);
                foreach (var seed in panel.Seeds)
                {
                    token.ThrowIfCancellationRequested();
                    var request = new TowerPanelTrial(scopeHash, panelHash, panel.Role, party.Id, charged + 1, seed, scenario);
                    charged++; // Charge even if the checkpoint/evaluator throws, cancels or returns invalid evidence.
                    Checkpoint(); // Allows the caller to persist an attempt before dispatch; never retried here.
                    token.ThrowIfCancellationRequested();
                    var outcome = await evaluate(Copy(request), token);
                    ValidateOutcome(request, outcome, trials);
                    observations.Add(new(request, outcome));
                    Checkpoint();
                }
            }
            token.ThrowIfCancellationRequested();
            var scores = Score(parties, observations);
            var measuredReferences = references.Where(r => parties.Any(p => p.Id == r.Id)).ToArray();
            var contrasts = Contrasts(parties, measuredReferences, observations);
            var result = new TowerPanelEvaluation(freeze, true, observations, scores, contrasts);
            panels[index] = result;
            Checkpoint();
            return result;
        }

        try
        {
            PartyChoice[] beam = [];
            for (var wave = 0; wave < 2; wave++)
            {
                token.ThrowIfCancellationRequested();
                var children = Copy(generate(wave + 1, Copy(beam), Copy(panels)));
                token.ThrowIfCancellationRequested();
                if (children.Count != (wave == 0 ? 9 : 8))
                    throw new TowerRacingBatchExhaustedException("Bounded generation did not fill the declared challenger batch.");
                ValidateParties(d, seen.Concat(children).ToArray());
                seen.AddRange(children);
                var challengers = beam.Concat(children).ToArray();
                var screen = await Measure(references.Concat(challengers).ToArray());
                var decision = Prune(wave + 1, challengers, screen.Scores);
                decisions.Add(decision);
                var byId = challengers.ToDictionary(p => p.Id, StringComparer.Ordinal);
                var survivors = decision.SurvivorIds.Select(id => byId[id]).ToArray();
                var continuation = await Measure(references.Concat(survivors).ToArray());
                var survivorsWithReferences = references.Concat(survivors).ToArray();
                var ids = survivorsWithReferences.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
                // Old-wave observations are deliberately absent: carried parents and children
                // receive the same current-wave 16 seeds, regardless of their earlier history.
                var common = Score(survivorsWithReferences, screen.Observations
                    .Where(o => ids.Contains(o.Request.PartyId)).Concat(continuation.Observations).ToArray());
                beam = Rank(common.Where(s => !referenceIds.Contains(s.Id))).Select(s => byId[s.Id]).ToArray();
                decisions[wave] = decision with { CommonScores = common, BeamIds = beam.Select(p => p.Id).ToArray() };
                Checkpoint();
            }
            var nominatedIds = references.Concat(beam.Take(2)).Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            nominees = Rank(decisions[1].CommonScores.Where(s => nominatedIds.Contains(s.Id))).Select(s => s.Id).ToArray();
            var allParties = references.Concat(beam).ToDictionary(p => p.Id, StringComparer.Ordinal);
            var selection = await Measure(nominees.Select(id => allParties[id]).ToArray());
            if (validationBenchmarkId is not null)
            {
                var challenger = TowerBenchmarkValidation.SelectChallenger(selection.Scores, nominees, primaryId, validationBenchmarkId,
                    version == TowerAffinitySearch.GeneratedNominationVersion ? referenceIds : null);
                validationFreeze = new(TowerBenchmarkValidation.Version, planHash, HarnessJson.Hash(selection.Freeze),
                    challenger, validationBenchmarkId);
                // Measure checkpoints this decision and the entire two-party panel before
                // any validation request. All 120 fights run regardless of nomination.
                var validation = await Measure([allParties[challenger], allParties[validationBenchmarkId]]);
                validationDecision = TowerBenchmarkValidation.Decide(validationFreeze, validation);
                selected = validationDecision.SelectedId;
            }
            else
                selected = positiveTieBenchmarkId is null ? Select(selection.Scores, nominees, primaryId)
                    : SelectBenchmarkPositiveTie(selection.Scores, nominees, primaryId, positiveTieBenchmarkId);
            if (charged != PlannedEvaluations) throw new InvalidDataException("Completed racing cost differs from 528.");
            token.ThrowIfCancellationRequested();
            return Snapshot("Complete");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            selected = null;
            validationDecision = null;
            return Snapshot("Cancelled", "Evaluation cancelled; partial panels cannot supply fitness or a selected output.");
        }
        catch (TowerRacingBatchExhaustedException ex)
        {
            return Snapshot("Incomplete", ex.Message);
        }
        catch (Exception ex)
        {
            selected = null;
            validationDecision = null;
            return Snapshot("Failed", ex.Message);
        }
    }

    // This is deterministic archive reconstruction, not combat replay or a resume path.
    // The caller retains the plan/report; this layer does not write or trust a disk journal.
    public static async Task<TowerBatchRacingReport> ReconstructAsync(TowerBatchRacingPlan plan,
        TowerBatchRacingReport saved, CancellationToken token = default)
    {
        plan = Copy(plan);
        saved = Copy(saved);
        Validate(plan);
        if (saved.Version != Version || saved.Status != "Complete" || saved.PlanHash != HarnessJson.Hash(plan))
            throw new InvalidDataException("Only a complete report from this exact versioned plan can be reconstructed.");
        var observations = saved.Panels.SelectMany(p => p.Observations).ToArray();
        var index = 0;
        var rebuilt = await RunAsync(plan, (request, _) => {
            if (index >= observations.Length || HarnessJson.Hash(observations[index].Request) != HarnessJson.Hash(request))
                throw new InvalidDataException("Archived request order or combat identity differs.");
            return Task.FromResult(observations[index++].Outcome);
        }, token);
        token.ThrowIfCancellationRequested();
        if (index != observations.Length || HarnessJson.Hash(rebuilt) != HarnessJson.Hash(saved))
            throw new InvalidDataException("Archived panels, observations, survivor decisions, selection or accounting differ.");
        return rebuilt;
    }

    private static TowerPanelScore[] Score(IReadOnlyList<PartyChoice> parties, IReadOnlyList<TowerPanelObservation> observations)
        => parties.Select(p => {
            var rows = observations.Where(o => o.Request.PartyId == p.Id).Select(o => o.Outcome).ToArray();
            var wins = rows.Where(o => o.Outcome == BattleOutcome.Victory).ToArray();
            var duration = wins.Length == 0 ? double.MaxValue : wins.Average(o => o.DurationSeconds);
            if (!double.IsFinite(duration)) throw new InvalidDataException("Winning duration aggregation overflowed.");
            return new TowerPanelScore(p.Id, rows.Length, wins.Length, rows.Count(o => o.Outcome == BattleOutcome.Draw),
                new((double)wins.Length / rows.Length, rows.Average(o => o.GuardianHealth), rows.Average(o => o.Survival),
                    duration));
        }).ToArray();

    // The existing discovery fitness ordering, on a complete common single-context panel.
    internal static IOrderedEnumerable<TowerPanelScore> Rank(IEnumerable<TowerPanelScore> rows) => rows
        .OrderByDescending(r => r.Fitness.WorstContextWinRate).ThenBy(r => r.Fitness.GuardianHealth)
        .ThenByDescending(r => r.Fitness.Survival).ThenBy(r => r.Fitness.VictoryDuration).ThenBy(r => r.Id, StringComparer.Ordinal);

    private static TowerPanelContrast[] Contrasts(IReadOnlyList<PartyChoice> parties,
        IReadOnlyList<PartyChoice> references, IReadOnlyList<TowerPanelObservation> observations)
    {
        var byParty = observations.GroupBy(o => o.Request.PartyId).ToDictionary(g => g.Key,
            g => g.ToDictionary(o => o.Request.Seed, o => o.Outcome.Outcome == BattleOutcome.Victory));
        return parties.SelectMany(p => references.Where(r => r.Id != p.Id).Select(r => {
            var candidate = byParty[p.Id]; var reference = byParty[r.Id];
            return new TowerPanelContrast(p.Id, r.Id, candidate.Count,
                candidate.Count(s => s.Value && !reference[s.Key]), candidate.Count(s => !s.Value && reference[s.Key]));
        })).ToArray();
    }

    private static TowerRacingDecision Prune(int wave, IReadOnlyList<PartyChoice> challengers,
        IReadOnlyList<TowerPanelScore> screen)
    {
        var parties = challengers.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var ranked = Rank(screen.Where(s => parties.ContainsKey(s.Id))).ToArray();
        var elite = ranked.Take(3).Select(s => s.Id).ToArray();
        var cutoff = ranked[2].Wins - 1;
        int Distance(string id) => elite.Min(e => TowerSuppliedCompositionSearch.Distance(parties[id], parties[e]) / 2);
        var eligible = ranked.Skip(3).Where(s => s.Wins >= cutoff).ToArray();
        // Stable OrderBy retains the existing fitness ordering for equal distances.
        var diverse = eligible.OrderByDescending(s => Distance(s.Id)).FirstOrDefault() ?? ranked[3];
        var survivors = elite.Append(diverse.Id).ToArray();
        return new(wave, elite, diverse.Id, Distance(diverse.Id), cutoff, eligible.Length == 0,
            survivors, ranked.Where(s => !survivors.Contains(s.Id)).Select(s => s.Id).ToArray(), [], []);
    }

    internal static string Select(IReadOnlyList<TowerPanelScore> scores, IReadOnlyList<string> nominees, string primaryId)
    {
        var ranks = nominees.Select((id, rank) => (id, rank)).ToDictionary(x => x.id, x => x.rank, StringComparer.Ordinal);
        var ranked = TowerZeroWinSelection.Rank(scores, s => s.Wins, s => s.Fitness.GuardianHealth,
            s => ranks[s.Id], s => s.Id).ToArray();
        var best = ranked[0];
        return best.Wins > 0 && ranked.Any(s => s.Id == primaryId && s.Wins == best.Wins) ? primaryId : best.Id;
    }

    // Keep the original primary override when the benchmark is below the leaders,
    // and the original health/nominee ordering when there are no wins.
    internal static string SelectBenchmarkPositiveTie(IReadOnlyList<TowerPanelScore> scores,
        IReadOnlyList<string> nominees, string primaryId, string benchmarkId)
    {
        var benchmark = scores.Single(s => s.Id == benchmarkId);
        var maximum = scores.Max(s => s.Wins);
        return maximum > 0 && benchmark.Wins == maximum ? benchmarkId : Select(scores, nominees, primaryId);
    }
}

internal sealed class TowerRacingBatchExhaustedException(string message) : Exception(message);
