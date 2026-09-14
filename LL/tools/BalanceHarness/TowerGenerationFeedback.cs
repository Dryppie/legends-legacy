namespace BalanceHarness;

public sealed record BossFeedbackRound(int AfterCandidates, IReadOnlyList<BossDiscoveryMeasurement> Measurements);

/// <summary>Fixed-budget training feedback. No reference recipes, rescreen trials or confirmation outcomes enter here.</summary>
public static class TowerGenerationFeedback
{
    public const string Version = "independent-generation-feedback-v14";
    public const string Method = "feedback-loadout-composition-joint";
    public static readonly string[] Methods = ["loadout-composition-joint", Method];
    public static readonly int[] Checkpoints = [96, 160, 224, 288];

    internal static void ValidateInputs(BossDiscoveryInputs d)
    {
        if (d.Generation.PolicyVersion != Version)
        {
            if (d.FeedbackSeeds is not null) throw new InvalidDataException("Feedback schedules require v14.");
            return;
        }
        if (d.FeedbackSeeds is null || !d.FeedbackSeeds.Keys.Order().SequenceEqual(d.DiscoverySeeds.Keys.Order())
            || d.DiscoverySeeds.Values.Any(s => s.Count != 8) || d.FeedbackSeeds.Values.Any(s => s is null || s.Count != 32)
            || d.FeedbackSeeds.Values.SelectMany(s => s).Distinct().Count() != 32 * d.FeedbackSeeds.Count
            || d.FeedbackSeeds.Values.SelectMany(s => s).Intersect(d.DiscoverySeeds.Values.SelectMany(s => s).Concat(d.Generation.Seeds)).Any()
            || d.Generation.Seeds.Intersect(d.DiscoverySeeds.Values.SelectMany(s => s)).Any())
            throw new InvalidDataException("Feedback needs 32 distinct training seeds per context, separate from discovery and generation.");
    }

    internal static void ValidateMeasurement(BossDiscoveryInputs d, BossDiscoveryMeasurement row, string id)
    {
        if (row is null || row.Id != id || row.Fitness is null || row.Behavior is null
            || row.Fitness != TowerBossGeneration.Fitness(d with { DiscoverySeeds = d.FeedbackSeeds! }, row.Cells, row.Fitness.VictoryDuration)
            || new[] { row.Behavior.SummonActiveTicks, row.Behavior.HealthDeficit, row.Behavior.DamagePrevented,
                row.Behavior.Healing, row.Behavior.DeniedTicks }.Any(v => !double.IsFinite(v) || v < 0))
            throw new InvalidDataException("A complete separately sampled training measurement is required.");
    }

    public static string[] Choose(BossDiscoveryInputs d, IReadOnlyList<BossDiscoveryMeasurement> rows,
        IReadOnlyList<BossFeedbackRound> rounds)
    {
        var used = rounds.SelectMany(r => r.Measurements).Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        // Freeze the four best unprobed recipes together. A result within this round cannot change its remaining probes.
        var selected = TowerBossGeneration.Rank(Effective(d, rows, rounds)).Where(r => !used.Contains(r.Id)).Take(4).Select(r => r.Id).ToArray();
        if (selected.Length != 4) throw new InvalidDataException("Feedback round lacks four distinct completed candidates.");
        return selected;
    }

    public static IReadOnlyList<BossDiscoveryMeasurement> Effective(BossDiscoveryInputs d,
        IReadOnlyList<BossDiscoveryMeasurement> rows, IReadOnlyList<BossFeedbackRound>? rounds)
    {
        if (rounds is null || rounds.Count == 0) return rows;
        var extra = rounds.SelectMany(r => r.Measurements).ToDictionary(r => r.Id, StringComparer.Ordinal);
        if (extra.Keys.Except(rows.Select(r => r.Id)).Any()) throw new InvalidDataException("Feedback must descend from this arm's completed candidates.");
        return rows.Select(row => extra.TryGetValue(row.Id, out var fresh) ? Combine(d, row, fresh) : row).ToArray();
    }

    internal static BossDiscoveryMeasurement Combine(BossDiscoveryInputs d, BossDiscoveryMeasurement original, BossDiscoveryMeasurement fresh)
    {
        ValidateMeasurement(d, fresh, original.Id);
        var combinedSeeds = d.DiscoverySeeds.ToDictionary(p => p.Key,
            p => (IReadOnlyList<int>)p.Value.Concat(d.FeedbackSeeds![p.Key]).ToArray());
        var cells = original.Cells.Select(c => {
            var f = fresh.Cells.Single(x => x.Context == c.Context); var n = c.Clears.Count + f.Clears.Count;
            return new PartyFloorScore(c.Context, c.Floor, c.Clears.Concat(f.Clears).ToArray(), c.Draws + f.Draws,
                (c.GuardianHealth * c.Clears.Count + f.GuardianHealth * f.Clears.Count) / n,
                (c.Survival * c.Clears.Count + f.Survival * f.Clears.Count) / n, c.Trials.Concat(f.Trials).ToArray());
        }).ToArray();
        var originalWins = original.Cells.Sum(c => c.Clears.Count(w => w)); var freshWins = fresh.Cells.Sum(c => c.Clears.Count(w => w));
        var duration = originalWins + freshWins == 0 ? double.MaxValue
            : (originalWins * original.Fitness.VictoryDuration + freshWins * fresh.Fitness.VictoryDuration) / (originalWins + freshWins);
        double Mean(Func<BossBehavior, double> get) => .2 * get(original.Behavior) + .8 * get(fresh.Behavior);
        var behavior = new BossBehavior(Mean(b => b.SummonActiveTicks), Mean(b => b.HealthDeficit), Mean(b => b.DamagePrevented),
            Mean(b => b.Healing), Mean(b => b.DeniedTicks), original.Behavior.Recovery is null || fresh.Behavior.Recovery is null ? null
                : new(Mean(b => b.Recovery!.FriendlyRegeneration), Mean(b => b.Recovery!.GuardianHealing), Mean(b => b.Recovery!.GuardianRegeneration)));
        return new(original.Id, TowerBossGeneration.Fitness(d with { DiscoverySeeds = combinedSeeds }, cells, duration), cells, behavior);
    }

    public static void ValidateRounds(BossDiscoveryInputs d, BossGenerationArm arm)
    {
        if (arm.Method != Method)
        {
            if (arm.Feedback is not null) throw new InvalidDataException("The unchanged comparator cannot consume feedback.");
            return;
        }
        if (arm.Feedback is null || !arm.Feedback.Select(r => r.AfterCandidates).SequenceEqual(Checkpoints))
            throw new InvalidDataException("The complete four-round training allocation is required.");
        var prior = new List<BossFeedbackRound>();
        foreach (var round in arm.Feedback)
        {
            if (!round.Measurements.Select(r => r.Id).SequenceEqual(Choose(d, arm.Evaluations.Take(round.AfterCandidates).ToArray(), prior)))
                throw new InvalidDataException("Training candidates or checkpoint order differ from the frozen rule.");
            foreach (var row in round.Measurements) ValidateMeasurement(d, row, row.Id);
            prior.Add(round);
        }
        _ = Effective(d, arm.Evaluations, prior);
    }
}
