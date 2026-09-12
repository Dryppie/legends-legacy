using System.Text.Json;
using Domain.Models.Combat;

namespace BalanceHarness;

/// <summary>The existing adaptive generator and fitness policy, backed by resumable compact seed batches.</summary>
public static class TowerCompactDiscovery
{
    public const string Kind = "tower-compact-discovery-v1";
    private sealed record Observation(string Context, string Trial, bool Win, BattleOutcome Outcome,
        double GuardianHealth, double Survival, double Duration, BossBehavior Behavior);

    public static async Task<BossDiscoveryRunReport> RunAsync(string root, string output, TowerBossDiscoveryDefinition definition,
        TowerBulkOptions options, bool resume = false, CancellationToken token = default, Action<string>? progress = null,
        bool verifyOnly = false)
    {
        var d = JsonSerializer.Deserialize<TowerBossDiscoveryDefinition>(JsonSerializer.SerializeToUtf8Bytes(definition, HarnessJson.Options), HarnessJson.Options)!;
        var cost = verifyOnly ? TowerBossDiscovery.Validate(d) : TowerBossDiscovery.Validate(root, d);
        using var campaign = TowerBulkCampaign.Open(root, output, Kind, d, d.ContentHashes, d.SettingsHash, d.ExecutionHash,
            cost.Discovery, d.MaximumBattles, options, resume, verifyOnly, token, progress);
        var inputs = TowerBossImprovement.Inputs(d);
        var inventory = TowerBossInventory.Create(campaign.Root, campaign.Contract.Scope.Settings.Threat);
        var mechanics = TowerBossPartyGenerator.FromInventory(inputs, inventory);
        campaign.Result("generation-inputs.json", inputs);
        campaign.Result("generation-mechanics.json", mechanics);
        var batch = 0; var logicalTrials = 0;
        var generation = await TowerBossImprovement.ExecuteAsync(d, inputs, mechanics, async (party, arm, ct) => {
            TowerBossDiscovery.ValidateParty(d, party);
            var contexts = inputs.DiscoverySeeds.OrderBy(p => p.Key, StringComparer.Ordinal).ToArray();
            var cases = contexts.Select((p, i) => new TowerCompactCase("context-" + i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture),
                TowerBossDiscovery.Scenario(d, p.Key, party, p.Value))).ToArray();
            var contextNames = cases.Select((c, i) => (c.Id, Context: contexts[i].Key)).ToDictionary(p => p.Id, p => p.Context);
            var id = "discovery-" + (batch++).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
            var observations = new List<Observation>();
            // Only scalar scoring inputs survive each verified report. The generator never holds an archive of full reports.
            await campaign.BatchAsync(id, cases, (context, trial) => {
                var report = trial.Report;
                observations.Add(new(contextNames[context], $"trial-{++logicalTrials:D6}", report.Succeeded, report.Battle.Summary.ContentOutcome,
                    (double)report.GuardianHealthRemainingPercent, TowerBenchmark.Survival(report), report.Battle.Summary.DurationSeconds,
                    TowerBossSearch.Observe([report], recovery: true)));
            });
            var cells = contexts.Select(c => {
                var rows = observations.Where(o => o.Context == c.Key).ToArray();
                return new PartyFloorScore(c.Key, d.Budget.PriorityFloor, rows.Select(o => o.Win).ToArray(),
                    rows.Count(o => o.Outcome == BattleOutcome.Draw), rows.Average(o => o.GuardianHealth), rows.Average(o => o.Survival),
                    rows.Select(o => o.Trial).ToArray());
            }).ToArray();
            var wins = observations.Where(o => o.Win).ToArray();
            double Average(Func<BossBehavior, double> get) => observations.Average(o => get(o.Behavior));
            var behavior = new BossBehavior(Average(b => b.SummonActiveTicks), Average(b => b.HealthDeficit), Average(b => b.DamagePrevented),
                Average(b => b.Healing), Average(b => b.DeniedTicks), new(Average(b => b.Recovery!.FriendlyRegeneration),
                    Average(b => b.Recovery!.GuardianHealing), Average(b => b.Recovery!.GuardianRegeneration)));
            progress?.Invoke($"Compact discovery: {batch} evaluated parties; {logicalTrials}/{cost.Discovery} logical trials.");
            return new BossDiscoveryMeasurement(party.Id,
                TowerBossGeneration.Fitness(inputs, cells, wins.Length == 0 ? double.MaxValue : wins.Average(o => o.Duration)), cells, behavior);
        }, campaign.Token);
        TowerBossDiscovery.ValidateProvenance(d, generation.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
        var report = new BossDiscoveryRunReport(generation.Status, cost.Discovery, logicalTrials, 0, generation, generation.Error);
        campaign.Result("discovery.json", report);
        campaign.TextResult("discovery.md", TowerBossDiscoveryRun.Markdown(report));
        if (report.Status is "Complete" or "Incomplete")
        {
            if (report.Status == "Complete" && logicalTrials != cost.Discovery
                || logicalTrials != generation.Arms.Sum(a => a.Evaluations.Count) * inputs.DiscoverySeeds.Values.Sum(s => s.Count))
                throw new InvalidDataException("Compact discovery logical accounting differs from the generator.");
            campaign.Result("shortlist.json", generation.DiscoveryShortlist.SelectMany(p => inputs.DiscoverySeeds.Select(c =>
                new TowerCompactCase(p.Id + "-" + c.Key, TowerBossDiscovery.Scenario(d, c.Key, p, c.Value)))).ToArray());
            campaign.Finish(logicalTrials);
        }
        else campaign.Failure(report.Status, report.Error);
        return report;
    }

    public static Task<BossDiscoveryRunReport> VerifyAsync(string output, CancellationToken token = default)
    {
        var contract = TowerContractJson.Read<TowerBulkContract>(Path.Combine(output, TowerBulkCampaign.ContractFile));
        if (contract.Kind != Kind) throw new InvalidDataException("Not a compact discovery campaign.");
        var d = contract.Definition.Deserialize<TowerBossDiscoveryDefinition>(HarnessJson.Options)!;
        return RunAsync(Path.Combine(output, "content"), output, d, contract.Options, resume: true, token: token, verifyOnly: true);
    }
}
