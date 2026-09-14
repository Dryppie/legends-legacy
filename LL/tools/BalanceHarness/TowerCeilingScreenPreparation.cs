using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BalanceHarness;

public sealed record TowerCeilingVariant(string Id, decimal Factor, string ContentRoot);
public sealed record TowerCeilingPreparationRequest(string Version, string CapturedRoot,
    string SourceManifestHash, string LedgerHash, IReadOnlyList<TowerCeilingVariant> Variants);
public sealed record TowerCeilingPreparedCell(string Id, string RecipeHash, string InputHash, string ParticipantsHash);
public sealed record TowerCeilingPreparedVariant(string Id, decimal Factor, string ContentHash,
    IReadOnlyList<TowerCeilingPreparedCell> Cells);
public sealed record TowerCeilingPreparationReceipt(string Status, int NewFights, int NewSeeds, int Reservations,
    int ProbeSeed, int PreparedCells, string RequestHash, ExecutionIdentity Execution,
    IReadOnlyList<TowerCeilingPreparedVariant> Variants, double Seconds, IReadOnlyList<TowerStageTiming> Timings);
public sealed record TowerCeilingFactorAssessment(decimal Factor, int MaximumWins, bool Eligible);

/// <summary>Fixed screen design. Preparation deliberately produces no executable seed schedule.</summary>
public static class TowerCeilingScreenContract
{
    public const string Version = "tower-captured-v19-ceiling-screen-v1";
    public const int Recipes = 253, Samples = 128, Cells = Recipes * 4, MaximumFights = Cells * Samples;
    public const decimal Health = 3.5366243328m, Offense = 4.4702934848m;
    public static IReadOnlyList<decimal> Factors => new decimal[] { 1m, 1.04m, 1.08m, 1.12m };

    public static string VariantId(int ordinal) => "scale-" + (100 + ordinal * 4);

    public static JsonNode ContentVariant(JsonNode baseline, decimal factor)
    {
        if (!Factors.Contains(factor)) throw new InvalidDataException("Factor is outside the frozen grid.");
        var result = baseline.DeepClone();
        var floor = result["floors"]!.AsArray().Single(f => f!["floorNumber"]!.GetValue<int>() == 5)!;
        var scaling = floor["guardianScaling"]!;
        if (scaling["health"]!.GetValue<decimal>() != Health || scaling["offense"]!.GetValue<decimal>() != Offense)
            throw new InvalidDataException("Scaling must start at the captured baseline; never compound factors.");
        scaling["health"] = Health * factor; scaling["offense"] = Offense * factor;
        return result;
    }

    internal static void VerifyParticipants(JsonElement baseline, JsonElement candidate, decimal factor)
    {
        if (!Factors.Contains(factor)) throw new InvalidDataException("Unknown factor.");
        var a = JsonNode.Parse(baseline.GetRawText())!.AsArray();
        var b = JsonNode.Parse(candidate.GetRawText())!.AsArray();
        if (a.Count != 11 || b.Count != 11) throw new InvalidDataException("Expected ten characters and Kharad.");
        var oldGuardian = a.Single(x => x!["slot"]!["side"]!.GetValue<string>() == "Hostile")!;
        var guardian = b.Single(x => x!["slot"]!["side"]!.GetValue<string>() == "Hostile")!;
        foreach (var attribute in new[] { "Power", "MaxHealth" })
        {
            var expected = oldGuardian["combatAttributes"]![attribute]!.GetValue<double>() * (double)factor;
            var actual = guardian["combatAttributes"]![attribute]!.GetValue<double>();
            if (!double.IsFinite(actual) || Math.Abs(actual - expected) > Math.Max(0.001, expected * 0.00001))
                throw new InvalidDataException("Materialized guardian scaling differs: " + attribute);
        }
        if (guardian["health"]!.GetValue<int>() != (int)guardian["combatAttributes"]!["MaxHealth"]!.GetValue<double>())
            throw new InvalidDataException("Guardian starting health differs from materialized maximum.");
        guardian["health"] = oldGuardian["health"]!.DeepClone();
        foreach (var attribute in new[] { "Power", "MaxHealth" })
            guardian["combatAttributes"]![attribute] = oldGuardian["combatAttributes"]![attribute]!.DeepClone();
        if (!JsonNode.DeepEquals(a, b)) throw new InvalidDataException("Preparation changed participants outside guardian Health/Power.");
    }

    public static TowerBalanceDefinition Template(TowerBalanceDefinition captured, string id,
        IReadOnlyDictionary<string, string> contentHashes, string settingsHash, string executionHash)
    {
        TowerBalanceEvaluator.Validate(captured);
        if (captured.Id != TowerPortfolioConfirmation.Policy || captured.Cells.Count != Recipes)
            throw new InvalidDataException("Use the complete captured portfolio.");
        return captured with { SchemaVersion = 1, Id = id, ContentHashes = contentHashes, SettingsHash = settingsHash,
            ExecutionHash = executionHash, MaximumBattles = Recipes * Samples,
            Cells = captured.Cells.Select(c => c with { Scenario = c.Scenario with { Seeds = [] }, MinimumSamples = Samples }).ToArray() };
    }

    // Pure whole-screen selection; called only on all four complete vectors. No per-factor alpha reset.
    // Existing staged arithmetic uses alpha .025: .025/506 == .05/1012, including both tails.
    // This reuses identical Wilson math without raising the ordinary evaluator's 1,000-cell limit.
    public static RateEstimate ScreenInterval(int wins) => TowerStagedBalance.Interval(wins, Samples, Cells / 2);

    public static IReadOnlyList<TowerCeilingFactorAssessment> Assess(IReadOnlyList<IReadOnlyList<int>> wins)
    {
        if (wins.Count != 4 || wins.Any(w => w.Count != Recipes || w.Any(n => n < 0 || n > Samples)))
            throw new InvalidDataException("Selection needs all 1,012 complete cells.");
        return wins.Select((w, i) => {
            var intervals = w.Select(ScreenInterval).ToArray();
            var maximum = w.Max();
            return new TowerCeilingFactorAssessment(Factors[i], maximum, i != 0 && maximum / (double)Samples is >= .15 and <= .40
                && intervals.All(r => r.Upper <= .5) && intervals.Any(r => r.Lower >= .1));
        }).ToArray();
    }

    public static decimal? Select(IReadOnlyList<IReadOnlyList<int>> wins) => Assess(wins).Where(r => r.Eligible)
        .OrderBy(r => Math.Abs(r.MaximumWins / (double)Samples - .3)).ThenBy(r => r.Factor).Select(r => (decimal?)r.Factor).FirstOrDefault();
}

/// <summary>Prepare all cells with one historical probe. No seed allocator or combat command is exposed.</summary>
public static class TowerCeilingScreenPreparation
{
    public const int MaximumSeconds = 900;
    public const long MaximumBytes = 1073741824;

    public static async Task<TowerCeilingPreparationReceipt> PrepareAsync(string requestPath, string output, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        output = Path.GetFullPath(output); using var lease = TowerCompactBundle.AcquireWriter(output);
        if (Path.Exists(output)) throw new IOException("Use a new preparation output; no retry or resume.");
        var request = HarnessJson.Read<TowerCeilingPreparationRequest>(requestPath);
        if (request.Version != TowerCeilingScreenContract.Version || request.Variants.Count != 4
            || !request.Variants.Select(v => v.Factor).SequenceEqual(TowerCeilingScreenContract.Factors)
            || !request.Variants.Select(v => v.Id).SequenceEqual(Enumerable.Range(0, 4).Select(TowerCeilingScreenContract.VariantId)))
            throw new InvalidDataException("Preparation requires the fixed four-factor design.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(TimeSpan.FromSeconds(MaximumSeconds));
        var ct = timeout.Token; var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight."));
        using var active = trace.Activate(); var clock = Stopwatch.StartNew();
        var root = Path.GetFullPath(request.CapturedRoot); string Source(string n) => Path.Combine(root, n);
        if (HarnessJson.FileHash(Source("final-files.json")) != request.SourceManifestHash
            || HarnessJson.FileHash(Source("seed-ledger.json")) != request.LedgerHash) throw new InvalidDataException("Source binding changed.");
        TowerBulkCampaign.VerifyFiles(root, "final-files.json", true, ct);
        var captured = TowerBalanceEvaluator.Read(Source("definition.json")); TowerBalanceEvaluator.Validate(captured);
        var sourceContract = TowerContractJson.Read<TowerBulkContract>(Source("confirmation/campaign.json"));
        TowerPortfolioConfirmationArchive.RequireGameplay(sourceContract.Scope.Execution, ExecutionIdentity.Current());
        var ledger = HarnessJson.Read<TowerPortfolioConfirmationSeeds>(Source("seed-ledger.json"));
        if (ledger.Historical.Count != 480707 || ledger.Confirmation.Count != 512
            || ledger.Historical.Concat(ledger.Confirmation).Distinct().Count() != 481219
            || captured.Cells.Any(c => !c.Scenario.Seeds.SequenceEqual(ledger.Confirmation))) throw new InvalidDataException("Historical reservations differ.");
        var probe = ledger.Confirmation[0]; // Reused for materialization only; never a new reservation or future trial.
        Directory.CreateDirectory(output); File.Copy(requestPath, Path.Combine(output, "request.json"));
        var variants = new List<TowerCeilingPreparedVariant>(); var baselines = new Dictionary<string, JsonElement>();
        try
        {
            for (var ordinal = 0; ordinal < request.Variants.Count; ordinal++)
            {
                ct.ThrowIfCancellationRequested(); var variant = request.Variants[ordinal]; var contentRoot = Path.GetFullPath(variant.ContentRoot);
                var expectedFloor = TowerCeilingScreenContract.ContentVariant(JsonNode.Parse(File.ReadAllText(Source("content/Data/" + TowerBattleRunner.FloorFile)))!, variant.Factor);
                var actualFloor = JsonNode.Parse(File.ReadAllText(Path.Combine(contentRoot, "Data", TowerBattleRunner.FloorFile)));
                if (!JsonNode.DeepEquals(expectedFloor, actualFloor)) throw new InvalidDataException("Candidate content has edits outside the two allowed values.");
                var hashes = TowerCompactBundle.ContentHashes(contentRoot, ct);
                foreach (var (name, hash) in captured.ContentHashes)
                    if ((ordinal == 0 || name != TowerBattleRunner.FloorFile) && hashes[name] != hash) throw new InvalidDataException("Unexpected content change: " + name);
                var settings = TowerBundle.ReadSettings(contentRoot);
                if (HarnessJson.Hash(settings) != captured.SettingsHash) throw new InvalidDataException("Settings differ.");
                var runner = new TowerBattleRunner(contentRoot, new OfflineContent(contentRoot, settings.Threat));
                var directory = Path.Combine(output, variant.Id); Directory.CreateDirectory(directory);
                var template = TowerCeilingScreenContract.Template(captured, variant.Id, hashes, captured.SettingsHash, HarnessJson.Hash(ExecutionIdentity.Current()));
                // Include the entire unchanged exclusion ledger even though templates have no executable seeds.
                template = template with { ExcludedCombatSeeds = ledger.Historical.Concat(ledger.Confirmation).Order().ToArray() };
                HarnessJson.WriteNew(Path.Combine(directory, "definition-template.json"), template);
                var cells = new List<TowerCeilingPreparedCell>();
                foreach (var cell in captured.Cells)
                {
                    ct.ThrowIfCancellationRequested(); TowerBossDiscovery.ValidateEquipment(cell.Scenario.Party, captured.Cohorts[0].Budget, 10);
                    var input = runner.CreateInput(cell.Scenario with { Seeds = [probe] }, probe, settings.Threat, settings.CheckpointIntervalTicks);
                    var participants = IdleBattleRunner.DescribeParticipants(await runner.PrepareAsync(input, ct));
                    if (ordinal == 0) baselines.Add(cell.Id, participants.Clone());
                    TowerCeilingScreenContract.VerifyParticipants(baselines[cell.Id], participants, variant.Factor);
                    HarnessJson.WriteNew(Path.Combine(directory, cell.Id + ".json"), new { id = cell.Id, input, participants });
                    cells.Add(new(cell.Id, TowerBossDiscovery.RecipeHash(cell.Scenario.Party), HarnessJson.Hash(input), HarnessJson.Hash(participants)));
                    if (cells.Count % 32 == 0 && TowerBulkCampaign.StorageBytes(output, ct) > MaximumBytes) throw new InvalidDataException("Preparation storage limit exceeded.");
                }
                variants.Add(new(variant.Id, variant.Factor, HarnessJson.Hash(hashes), cells));
            }
            var receipt = new TowerCeilingPreparationReceipt("MaterializedSeedBindingPending", 0, 0, 481219, probe,
                variants.Sum(v => v.Cells.Count), HarnessJson.FileHash(requestPath), ExecutionIdentity.Current(), variants,
                clock.Elapsed.TotalSeconds, trace.Snapshot());
            HarnessJson.WriteNew(Path.Combine(output, "preparation.json"), receipt);
            if (receipt.PreparedCells != 1012 || TowerBulkCampaign.StorageBytes(output, ct) > MaximumBytes) throw new InvalidDataException("Incomplete or oversized preparation.");
            var files = TowerBulkCampaign.Paths(output).ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash);
            HarnessJson.WriteNew(Path.Combine(output, "prepared-files.json"), files);
            if (TowerBulkCampaign.StorageBytes(output, ct) > MaximumBytes) throw new InvalidDataException("Final preparation manifest exceeds cap.");
            TowerBulkCampaign.VerifyFiles(output, "prepared-files.json", true, ct); return receipt;
        }
        catch (Exception e)
        {
            HarnessJson.WriteNew(Path.Combine(output, "preparation-failure.json"), new { error = e.ToString(), seconds = clock.Elapsed.TotalSeconds, fights = 0, seeds = 0 }); throw;
        }
    }

    public static TowerCeilingPreparationReceipt Verify(string output, CancellationToken token = default)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Verification cannot fight.")).Activate();
        TowerBulkCampaign.VerifyFiles(output, "prepared-files.json", true, token);
        var receipt = HarnessJson.Read<TowerCeilingPreparationReceipt>(Path.Combine(output, "preparation.json"));
        if (receipt.Status != "MaterializedSeedBindingPending" || receipt.NewFights != 0 || receipt.NewSeeds != 0 || receipt.PreparedCells != 1012
            || receipt.RequestHash != HarnessJson.FileHash(Path.Combine(output, "request.json"))) throw new InvalidDataException("Invalid preparation receipt.");
        if (TowerBulkCampaign.StorageBytes(output, token) > MaximumBytes) throw new InvalidDataException("Prepared output exceeds cap.");
        return receipt;
    }
}
