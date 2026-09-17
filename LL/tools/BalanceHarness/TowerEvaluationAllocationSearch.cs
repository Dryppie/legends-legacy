using System.Globalization;
using System.Text.Json;
using Common.Randomness;

namespace BalanceHarness;

public sealed record BossEvaluationRound(int Index, IReadOnlyList<string> ParentsBefore,
    IReadOnlyList<string> NewCandidates, IReadOnlyList<int> ScreenSeeds, IReadOnlyList<int> PromotionSeeds,
    IReadOnlyList<BossDiscoveryMeasurement> Screening, IReadOnlyList<string> PromotedIds,
    IReadOnlyList<BossDiscoveryMeasurement> Promotion, IReadOnlyList<string> ParentsAfter);

/// <summary>Supplied-team search with fresh, equally sampled parent tournaments. Training never establishes strength.</summary>
public static class TowerEvaluationAllocationSearch
{
    public const string Version = "retained-composition-racing-v1";
    public const int BatchSize = 8, ParentCount = 4, ScreenSamples = 8, PromotionSamples = 16;
    internal delegate Task<BossDiscoveryMeasurement> PanelEvaluator(PartyChoice party, string arm,
        IReadOnlyList<int> seeds, CancellationToken token);

    public static int Rounds(BossDiscoveryGeneration generation) => generation.CandidatesPerArm / BatchSize;
    public static int RequiredSeeds(BossDiscoveryGeneration generation) => Rounds(generation) * (ScreenSamples + PromotionSamples);
    // At most four parents plus two protected starts enter each later batch.
    // Duplicated parents/starts reduce actual cost, never the declared reservation.
    public static int MaximumFights(BossDiscoveryGeneration generation) => checked(
        BatchSize * ScreenSamples + (ParentCount + 2) * PromotionSamples
        + (Rounds(generation) - 1) * ((BatchSize + ParentCount + 2) * ScreenSamples + (ParentCount + 2) * PromotionSamples));

    internal static void Validate(TowerBossDiscoveryDefinition d)
    {
        if (d.Generation.CandidatesPerArm is not (16 or 24 or 32)
            || d.Stages.Schedules.Values.Any(s => s.Discovery.Count != RequiredSeeds(d.Generation)))
            throw new InvalidDataException("Racing requires 16, 24 or 32 candidates and exactly 24 training seeds per eight-candidate round.");
    }

    internal static BossDiscoveryInputs PanelInputs(BossDiscoveryInputs inputs, IReadOnlyList<int> seeds)
        => inputs with { DiscoverySeeds = new Dictionary<string, IReadOnlyList<int>> {
            [inputs.DiscoverySeeds.Keys.Single()] = seeds.ToArray() } };

    internal static IEnumerable<BossDiscoveryMeasurement> Observations(BossGenerationArm arm)
        => arm.EvaluationRounds is null ? arm.Evaluations
            : arm.EvaluationRounds.SelectMany(r => r.Screening.Concat(r.Promotion));

    internal static int ActualFights(BossGenerationResult result) => result.Arms
        .SelectMany(Observations).Sum(r => r.Cells.Sum(c => c.Clears.Count));

    internal static IReadOnlyList<BossDiscoveryMeasurement> NominationMeasurements(BossGenerationArm arm)
        => arm.EvaluationRounds is null ? arm.Evaluations : arm.EvaluationRounds.Last().Promotion;

    internal static async Task<BossGenerationResult> RunAsync(TowerBossDiscoveryDefinition definition,
        BossGenerationMechanics mechanics, PanelEvaluator evaluate, CancellationToken token,
        Action<BossGenerationResult>? checkpoint = null)
    {
        ArgumentNullException.ThrowIfNull(evaluate);
        var d = JsonSerializer.Deserialize<TowerBossDiscoveryDefinition>(JsonSerializer.Serialize(definition, HarnessJson.Options), HarnessJson.Options)!;
        TowerBossDiscovery.Validate(d);
        if (d.Generation.PolicyVersion != Version) throw new InvalidDataException("Racing requires its explicit policy.");
        var inputs = TowerBossDiscovery.CopyGenerationInputs(d);
        var generator = new TowerBossPartyGenerator(inputs, mechanics);
        var seed = d.Generation.Seeds.Single(); var method = TowerSuppliedCompositionSearch.Baseline;
        var seedText = seed.ToString(CultureInfo.InvariantCulture); var armId = method + "-" + seedText;
        // Preserve the baseline construction streams and attempt-based operator/fresh schedule.
        var fresh = new Random(StableRandom.Seed(TowerSuppliedCompositionSearch.Version, seedText, "fresh"));
        var random = new Random(StableRandom.Seed(TowerSuppliedCompositionSearch.Version, seedText, method));
        var starts = d.Starts.OrderBy(s => s.Id, StringComparer.Ordinal).ToArray();
        var anchors = starts.Select(s => s.Party.Id).ToArray();
        var proposals = new List<BossGeneratedProposal>();
        var accepted = new Dictionary<string, BossGeneratedProposal>(StringComparer.Ordinal);
        var scans = new Dictionary<string, TowerSuppliedCompositionSearch.SingleNeighborhood>(StringComparer.Ordinal);
        var rows = new List<BossDiscoveryMeasurement>(); var rounds = new List<BossEvaluationRound>();
        var parents = anchors; var status = "Incomplete"; var stop = "Running"; string? error = null;
        PartyChoice[] shortlist = [];
        BossGenerationResult Report() => new(Version, status,
            [new(method, seed, stop, proposals.ToArray(), rows.ToArray(), EvaluationRounds: rounds.ToArray())], shortlist, error);
        void Snapshot() => checkpoint?.Invoke(Report());
        try
        {
            Snapshot();
            for (var round = 0; round < Rounds(d.Generation); round++)
            {
                token.ThrowIfCancellationRequested();
                var before = parents.ToArray(); var batch = new List<string>();
                while (batch.Count < BatchSize && proposals.Count < d.Generation.MaximumAttemptsPerArm)
                {
                    token.ThrowIfCancellationRequested();
                    var attempt = proposals.Count; var turn = attempt - (starts.Length + 6);
                    string operation; string[] parentIds = [], references = [];
                    BossGeneratedChoice choice; BossGeneratedProposal? parent = null;
                    if (attempt < starts.Length)
                    {
                        var start = starts[attempt]; operation = "supplied";
                        parentIds = [start.Id]; references = [start.ReferenceId];
                        choice = new(start.Party, operation, null, null);
                    }
                    else if (turn < 0 || turn % 4 == 3)
                    { operation = "fresh-legal"; choice = TowerSuppliedCompositionSearch.Fresh(inputs, fresh); }
                    else
                    {
                        var choices = random.Next(4) == 0 ? anchors : before;
                        parent = accepted[choices[random.Next(choices.Length)]];
                        var mutation = turn - turn / 4;
                        operation = TowerSuppliedCompositionSearch.BaselineOperator(mutation);
                        BossGeneratedProposal? donor = null;
                        if (operation == "recombine")
                        {
                            var alternatives = before.Concat(anchors).Distinct().Where(id => id != parent.Party!.Id).ToArray();
                            if (alternatives.Length > 0) donor = accepted[alternatives[random.Next(alternatives.Length)]];
                            else operation = "single";
                        }
                        parentIds = donor is null ? [parent.Provenance.Id] : [parent.Provenance.Id, donor.Provenance.Id];
                        references = parent.Provenance.ReferenceIds.Concat(donor?.Provenance.ReferenceIds ?? []).Distinct().Order(StringComparer.Ordinal).ToArray();
                        if (operation == "single")
                        {
                            if (!scans.TryGetValue(parent.Party!.Id, out var scan))
                                scans.Add(parent.Party.Id, scan = new(inputs, parent.Party, random));
                            choice = scan.Next(inputs);
                        }
                        else choice = generator.Mutate(random, operation, parent.Party!, donor?.Party);
                    }
                    var rejection = choice.Rejection ?? (choice.Party is null ? "no-legal-proposal"
                        : accepted.ContainsKey(choice.Party.Id) ? "duplicate" : null);
                    var changed = choice.Party?.Builds.Where(p => parent is null || !p.Value.SequenceEqual(parent.Party!.Builds[p.Key]))
                        .Select(p => p.Key).Order().ToArray() ?? [];
                    var proposal = new BossGeneratedProposal(new($"{armId}-proposal-{attempt:D5}", seed, method, operation, parentIds, references),
                        choice.Party, choice.Intent, choice.Interaction, rejection ?? "proposed",
                        Supplied: new(before, changed, TowerSuppliedCompositionSearch.ConstructionChecks));
                    proposals.Add(proposal);
                    if (rejection is null)
                    {
                        TowerBossDiscovery.ValidateParty(d, choice.Party!);
                        accepted.Add(choice.Party!.Id, proposal); batch.Add(choice.Party.Id);
                    }
                    Snapshot();
                }
                if (batch.Count != BatchSize) { stop = "ProposalBudgetExhausted"; break; }

                var allSeeds = inputs.DiscoverySeeds.Single().Value;
                var screenSeeds = allSeeds.Skip(round * (ScreenSamples + PromotionSamples)).Take(ScreenSamples).ToArray();
                var promotionSeeds = allSeeds.Skip(round * (ScreenSamples + PromotionSamples) + ScreenSamples).Take(PromotionSamples).ToArray();
                var screening = new List<BossDiscoveryMeasurement>(); var promotion = new List<BossDiscoveryMeasurement>();
                string[] promoted = [], after = [];
                void SaveRound()
                {
                    var value = new BossEvaluationRound(round, before, batch.ToArray(), screenSeeds, promotionSeeds,
                        screening.ToArray(), promoted, promotion.ToArray(), after);
                    if (rounds.Count == round) rounds.Add(value); else rounds[round] = value;
                    Snapshot();
                }
                async Task<BossDiscoveryMeasurement> Measure(string id, IReadOnlyList<int> panel)
                {
                    token.ThrowIfCancellationRequested();
                    // The evaluator receives its own panel: caller mutation cannot change the frozen trace.
                    var row = await evaluate(accepted[id].Party!, armId, panel.ToArray(), token);
                    if (row is null || row.Id != id || row.Fitness is null
                        || row.Fitness != TowerBossGeneration.Fitness(PanelInputs(inputs, panel), row.Cells, row.Fitness.VictoryDuration)
                        || row.Behavior is null || new[] { row.Behavior.HealthDeficit, row.Behavior.Healing,
                            row.Behavior.DamagePrevented, row.Behavior.DeniedTicks, row.Behavior.SummonActiveTicks }.Any(v => !double.IsFinite(v) || v < 0))
                        throw new InvalidDataException("Racing requires a complete finite measurement on the requested panel.");
                    return row;
                }
                SaveRound();
                // The initial batch already contains the starts. Later rounds reevaluate parents and starts too.
                foreach (var id in before.Concat(anchors).Concat(batch).Distinct())
                {
                    var row = await Measure(id, screenSeeds); screening.Add(row);
                    if (batch.Contains(id))
                    {
                        rows.Add(row);
                        var original = accepted[id]; var completed = original with { Result = "evaluated" };
                        proposals[proposals.IndexOf(original)] = completed; accepted[id] = completed;
                    }
                    SaveRound();
                }
                promoted = TowerBossGeneration.Rank(screening).Take(ParentCount).Select(r => r.Id)
                    .Concat(anchors).Distinct().ToArray();
                SaveRound(); // Freeze the whole promotion batch before consuming its outcomes.
                foreach (var id in promoted) { promotion.Add(await Measure(id, promotionSeeds)); SaveRound(); }
                // Only this common fresh panel chooses parents; no old or mixed-size scores are ranked here.
                parents = after = TowerBossGeneration.Rank(promotion).Take(ParentCount).Select(r => r.Id).ToArray();
                SaveRound();
            }
            token.ThrowIfCancellationRequested();
            if (rounds.Count == Rounds(d.Generation) && rounds[^1].ParentsAfter.Count == ParentCount)
            {
                var ranked = TowerBossGeneration.Rank(rounds[^1].Promotion).ToArray();
                var nominees = anchors.Concat(ranked.Where(r => !anchors.Contains(r.Id)).Take(2).Select(r => r.Id)).ToHashSet();
                shortlist = ranked.Where(r => nominees.Contains(r.Id)).Select(r => accepted[r.Id].Party!).ToArray();
                if (shortlist.Length != 4) throw new InvalidDataException("Racing did not retain two distinct challengers and both supplied teams.");
                status = "Complete"; stop = "CandidateBudgetReached";
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        { status = "Cancelled"; stop = "Cancelled"; error = "Racing interrupted; partial rounds cannot nominate teams."; }
        catch (Exception exception) { status = "Invalid"; stop = "Invalid"; error = exception.GetType().Name + ": " + exception.Message; }
        if (status != "Complete") shortlist = [];
        TowerBossDiscovery.ValidateProvenance(d, proposals.Select(p => p.Provenance).ToArray());
        Snapshot();
        return Report();
    }
}
