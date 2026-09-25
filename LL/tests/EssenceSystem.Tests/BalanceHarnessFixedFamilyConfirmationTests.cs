using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using BalanceHarness.ProcessFixture;
using Domain.Models.Combat;
using Xunit.Abstractions;
using C = BalanceHarness.TowerFixedFamilyConfirmation;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed partial class BalanceHarnessFixedFamilyConfirmationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-fixed-family-fixture-"+Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Confirmation fixture entered combat.")).Activate();
    private readonly ITestOutputHelper output;
    public BalanceHarnessFixedFamilyConfirmationTests(ITestOutputHelper output) { this.output = output; Directory.CreateDirectory(root); }
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private static string Json(object value) => JsonSerializer.Serialize(value, HarnessJson.Options);
    private static string Repository()
    {
        for (var p = Directory.GetCurrentDirectory(); p is not null; p = Path.GetDirectoryName(p))
            if (File.Exists(Path.Combine(p, "Balance Harness/Tower-Practical-Fixed-Family-Confirmation-Plan.json"))) return p;
        throw new IOException("Run through the repository test wrapper.");
    }
    private static TowerSettings Settings => TowerBundle.ReadSettings(Path.Combine(Repository(), "LL/src/API/API.LL"));
    private static TowerFixedFamilyDefinition Definition()
    {
        var plan = HarnessJson.Read<JsonElement>(Path.Combine(Repository(), "Balance Harness/Tower-Practical-Fixed-Family-Confirmation-Plan.json"));
        var teams = plan.GetProperty("recipesInFixedExecutionOrder").EnumerateArray().Select(t => new TowerFixedFamily(t.GetProperty("role").GetString()!,
            t.GetProperty("partyId").GetString()!, t.TryGetProperty("referenceId", out var reference) ? [reference.GetString()!] : [], t.GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)!)).ToArray();
        return new(C.Version, teams, plan.GetProperty("contentHashes").Deserialize<Dictionary<string, string>>(HarnessJson.Options)!,
            plan.GetProperty("settingsHash").GetString()!, HarnessJson.Hash(ExecutionIdentity.Current()), [-987]);
    }
    [Fact]
    public void Typed_contract_authenticates_the_exact_frozen_plan()
    {
        var d = Definition();
        Assert.Equal(C.TeamsHash, HarnessJson.Hash(d.Teams)); Assert.Equal(C.ContentHash, HarnessJson.Hash(d.ContentHashes));
        Assert.Equal(C.SettingsHash, HarnessJson.Hash(Settings)); C.ValidateDefinition(d);
        output.WriteLine("Verified fixture producing runtime: "+Json(ExecutionIdentity.Current()));
    }
    private (TowerFixedFamilyRequest Q, TowerFixedFamilyInputs Input) Input()
    {
        var d = Definition(); C.ValidateDefinition(d);
        var prior = Path.Combine(root, "prior-seed-ledger.json"); HarnessJson.WriteNew(prior, new { historical = d.ExcludedCombatSeeds });
        var source = Path.Combine(root, "definition.json"); HarnessJson.WriteNew(source, d);
        var content = Path.Combine(root, "content"); Directory.CreateDirectory(content);
        var history = new Dictionary<string, string> { [prior] = HarnessJson.FileHash(prior) };
        var q = new TowerFixedFamilyRequest(C.Version, content, source, HarnessJson.FileHash(source), root, Path.Combine(root, "result"), history,
            1801, 1025L*1048576, new Dictionary<string, TowerDiagnosticPhaseLimit> { ["admission"] = new(90, 64L*1048576),
                ["combat"] = new(600, 512L*1048576), ["audit"] = new(600, 128L*1048576) }, 1, 1048576, [new("Literal prior fixture", 1, 1048576, prior, HarnessJson.FileHash(prior))]);
        C.ValidateRequest(q, true); return (q, new(d, new(history, [-987])));
    }
    private static TowerPracticalLaunch Launch(TowerFixedFamilyRequest q)
    {
        Directory.CreateDirectory(q.OutputRoot); var now = DateTimeOffset.UtcNow; using var parent = Process.GetCurrentProcess();
        var launch = new TowerPracticalLaunch(HarnessJson.Hash(q), now, now.AddSeconds(q.MaximumSeconds-q.PriorSeconds), parent.Id, parent.StartTime.ToUniversalTime().Ticks);
        HarnessJson.WriteNew(C.P(q, "request.json"), q); HarnessJson.WriteNew(C.P(q, "launch.json"), launch); return launch;
    }
    private async Task<(TowerFixedFamilyRequest Q, TowerFixedFamilyResult Result, TowerPracticalLaunch Launch)> Completed(string mode = "complete")
    {
        var (q, input) = Input(); var launch = Launch(q); var clock = Stopwatch.StartNew(); var draws = 0;
        var result = await C.RunOperation(q, launch, _ => input, (f, check, ct) => FixedFamilyFixtureHost.Prepare(q, f, Settings, check, ct),
            (f, panel, attempt, check, ct) => FixedFamilyFixtureHost.Study(q, f, panel, mode, attempt, check, ct),
            ct => FixedFamilyFixtureHost.Verify(q, ct), default, entropy: bytes => { draws++; FixedFamilyFixtureHost.Entropy(bytes); });
        Assert.Equal(1, draws); HarnessJson.WriteNew(C.P(q, "worker-result.json"), result); C.Publish(q, launch, clock, default);
        return (q, result, launch);
    }

    [Fact]
    public async Task Complete_44000_literal_reports_publish_reconstruct_and_count_without_combat_or_random_draws()
    {
        var (q, result, _) = await Completed();
        Assert.Equal("StrongerFixedCandidatesConfirmed", result.StrengthDecision); Assert.Equal("RecommendFixedCandidates", result.Adoption);
        Assert.Equal(8, result.Rates.Count); Assert.Equal(12, result.Contrasts.Count); Assert.Equal(2, result.RecommendedPartyIds.Count); Assert.Equal(2, result.ControlPartyIds.Count);
        Assert.Equal("NotAssessed", result.BalanceAssessment);
        var verified = await C.VerifyPublication(q.OutputRoot, ct => FixedFamilyFixtureHost.Verify(q, ct)); Assert.Equal(Json(result), Json(verified));
        await Assert.ThrowsAsync<InvalidOperationException>(() => C.VerifyPublication(q.OutputRoot, _ => {
            TowerPerformanceTrace.BattleStarted(); throw new Exception("Audit guard did not reject combat"); }));
        var before = TowerBulkCampaign.Paths(q.OutputRoot).ToDictionary(p => p, HarnessJson.FileHash);
        await Assert.ThrowsAnyAsync<Exception>(() => C.Verify(q.OutputRoot)); // Public native route cannot authenticate synthetic input preparation.
        Assert.All(before, p => Assert.Equal(p.Value, HarnessJson.FileHash(p.Key)));
        var panel = HarnessJson.Read<TowerFixedFamilyPanel>(C.P(q, "confirmation-binding.json"));
        Assert.Equal(5500, panel.Panel.Count); Assert.Equal(11000, panel.NewReservations.Count); Assert.Equal(5500, panel.Words.Count(w => w.Classification == "ReservedUnused"));
        Assert.Equal(11001, TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(C.P(q, "seed-ledger.json"))).Length);
        Assert.Equal(88000, File.ReadLines(C.P(q, "attempts.jsonl")).Count()); Assert.Equal(48, Directory.GetFiles(C.P(q, "study/recipes")).Length);
        Assert.All(HarnessJson.Read<JsonElement>(C.P(q, "teams.json")).GetProperty("teams").EnumerateArray(), t => Assert.Empty(t.GetProperty("scenario").GetProperty("seeds").EnumerateArray()));
        output.WriteLine("Literal confirmation resource observation: "+Json(new { closeout = HarnessJson.Read<JsonElement>(C.P(q, "closeout.json")),
            phases = C.PhaseNames.Select(n => HarnessJson.Read<TowerDiagnosticPhaseReceipt>(C.P(q, n+"-phase.json"))).ToArray(),
            actualCombat = 0, productionEntropyDraws = 0, scope = "Synthetic reports and inputs; no retained runtime/content or combat feasibility claim" }));

        // Re-signing inventories does not make altered scientific or operational bindings valid.
        var original = TowerBulkCampaign.Paths(q.OutputRoot).Where(p => !p.Contains(Path.DirectorySeparatorChar+"battles"+Path.DirectorySeparatorChar))
            .ToDictionary(p => p, File.ReadAllBytes);
        foreach (var change in new[] { "freeze", "chunk", "tail", "attempt", "event", "missing", "extra", "duplicate", "reordered", "input", "phase", "result", "native-audit", "teams", "prior-receipt" })
        {
            try
            {
                switch (change)
                {
                    case "freeze": Edit(C.P(q, "freeze.json"), n => n["requestHash"] = new string('0', 64)); break;
                    case "chunk": Edit(C.P(q, "chunks.json"), n => n[0]!["scenario"]!["id"] = "changed"); break;
                    case "tail": Edit(C.P(q, "seed-ledger.json"), n => n["reserved"]!.AsArray().RemoveAt(10999)); break;
                    case "attempt": File.AppendAllText(C.P(q, "attempts.jsonl"), "{}\n"); break;
                    case "event": File.AppendAllText(C.P(q, "events.jsonl"), File.ReadLines(C.P(q, "events.jsonl")).First()+"\n"); break;
                    case "missing": File.WriteAllLines(C.P(q, "study/trials.jsonl"), File.ReadAllLines(C.P(q, "study/trials.jsonl")).Skip(1)); break;
                    case "extra": File.AppendAllText(C.P(q, "study/trials.jsonl"), File.ReadLines(C.P(q, "study/trials.jsonl")).First()+"\n"); break;
                    case "duplicate": var duplicates = File.ReadAllLines(C.P(q, "study/trials.jsonl")); duplicates[1] = duplicates[0]; File.WriteAllLines(C.P(q, "study/trials.jsonl"), duplicates); break;
                    case "reordered": var lines = File.ReadAllLines(C.P(q, "study/trials.jsonl")); (lines[0], lines[1]) = (lines[1], lines[0]); File.WriteAllLines(C.P(q, "study/trials.jsonl"), lines); break;
                    case "input": var rows = File.ReadAllLines(C.P(q, "study/trials.jsonl")); var row = JsonNode.Parse(rows[0])!; row["inputHash"] = new string('0', 64); rows[0] = row.ToJsonString(new(HarnessJson.Options) { WriteIndented = false }); File.WriteAllLines(C.P(q, "study/trials.jsonl"), rows); break;
                    case "phase": Edit(C.P(q, "audit-phase.json"), n => n["chargedSeconds"] = 999); break;
                    case "result": Edit(C.P(q, "result.json"), n => n["adoption"] = "Hold"); break;
                    case "native-audit": Edit(C.P(q, "native-audit.json"), n => n["newFights"] = 1); break;
                    case "teams": Edit(C.P(q, "teams.json"), n => n["teams"]![0]!["scenario"]!["seeds"] = new JsonArray(3)); break;
                    case "prior-receipt": File.AppendAllText(C.P(q, "prior-charges/000.json"), " "); break;
                }
                Resign(q);
                // Late publication mismatches can use the already verified study; the independent count audit still runs.
                var study = HarnessJson.Read<TowerFixedFamilyStudy>(C.P(q, "study/study.json"));
                await Assert.ThrowsAnyAsync<Exception>(() => C.VerifyPublication(q.OutputRoot,
                    change is "result" or "native-audit" or "teams" ? _ => Task.FromResult(study) : ct => FixedFamilyFixtureHost.Verify(q, ct)));
            }
            finally { foreach (var p in original) File.WriteAllBytes(p.Key, p.Value); }
        }

        var savedStudy = HarnessJson.Read<TowerFixedFamilyStudy>(C.P(q, "study/study.json"));
        var bad = savedStudy with { Evidence = savedStudy.Evidence.Select((e, i) => i == 0 ? e with { Trials = e.Trials.Select((t, j) => j == 0 ? t with { Outcome = BattleOutcome.Draw } : t).ToArray() } : e).ToArray() };
        Assert.Throws<InvalidDataException>(() => C.IndependentAudit(q, bad, default));
        var firstReport = C.P(q, "study/battles/trial-000001.json.gz"); var reportBytes = File.ReadAllBytes(firstReport);
        try
        {
            var report = TowerLoadoutArchive.ReadBattle(C.P(q, "study"), "trial-000001", "gzip-json-v1"); File.Delete(firstReport);
            TowerLoadoutArchive.WriteBattle(C.P(q, "study"), "trial-000001", report with { Battle = report.Battle with { Seed = 42 } }, "gzip-json-v1"); Resign(q);
            await Assert.ThrowsAnyAsync<Exception>(() => FixedFamilyFixtureHost.Verify(q, default));
        }
        finally { File.WriteAllBytes(firstReport, reportBytes); foreach (var p in original) File.WriteAllBytes(p.Key, p.Value); }
    }

    private static void Edit(string path, Action<JsonNode> edit)
    { var node = JsonNode.Parse(File.ReadAllText(path))!; edit(node); File.WriteAllText(path, node.ToJsonString(HarnessJson.Options)); }
    private static void Resign(TowerFixedFamilyRequest q)
    {
        File.Delete(C.P(q, "study/files.json")); C.Seal(C.P(q, "study"));
        var final = HarnessJson.Read<TowerFixedFamilyCloseout>(C.P(q, "closeout.json")); File.Delete(C.P(q, "closeout.json"));
        File.Delete(C.P(q, "files.json")); C.Seal(q.OutputRoot);
        final = final with { FilesHash = HarnessJson.FileHash(C.P(q, "files.json")) };
        var bytes = TowerBulkCampaign.StorageBytes(q.OutputRoot, default);
        for (var i = 0; i < 10; i++) final = final with { RetainedBytes = bytes+JsonSerializer.SerializeToUtf8Bytes(final, HarnessJson.Options).Length };
        HarnessJson.WriteNew(C.P(q, "closeout.json"), final);
    }

    [Fact]
    public async Task Native_preparation_accepts_all_48_exact_chunks_without_relaxing_1000_seed_limit()
    {
        var d = Definition(); var seeds = Enumerable.Range(1, 5500).ToArray(); var chunks = C.Chunks(d, seeds);
        var settings = Settings; var native = Path.Combine(Repository(), "LL/src/API/API.LL"); var runner = new TowerBattleRunner(native, new OfflineContent(native, settings.Threat));
        Assert.Equal(48, chunks.Count);
        foreach (var team in chunks.GroupBy(c => c.TeamOrdinal))
        {
            Assert.Equal(seeds, team.SelectMany(c => c.Scenario.Seeds)); string? partyHash = null;
            foreach (var chunk in team)
            {
                Assert.Equal(C.SliceSizes[chunk.SliceOrdinal], chunk.Scenario.Seeds.Count);
                Assert.Equal(chunk.SeedFreeHash, HarnessJson.Hash(chunk.Scenario with { Seeds = [] }));
                foreach (var seed in new[] { chunk.Scenario.Seeds.First(), chunk.Scenario.Seeds.Last() })
                {
                    var input = runner.CreateInput(chunk.Scenario, seed, settings.Threat, settings.CheckpointIntervalTicks);
                    partyHash ??= HarnessJson.Hash(input.Party); Assert.Equal(partyHash, HarnessJson.Hash(input.Party)); Assert.Equal(seed, input.Rules.RandomSeed);
                }
            }
            var scenario = team.First().Scenario; await runner.PrepareAsync(runner.CreateInput(scenario, scenario.Seeds[0], settings.Threat, settings.CheckpointIntervalTicks));
            Assert.Throws<InvalidDataException>(() => runner.CreateInput(scenario with { Seeds = seeds }, seeds[0], settings.Threat, settings.CheckpointIntervalTicks));
        }
    }

    [Theory]
    [InlineData(620, 275, 0, false)] [InlineData(621, 275, 0, true)]
    [InlineData(2000, 274, 0, false)] [InlineData(2000, 275, 0, true)]
    [InlineData(2500, 2500, 2225, true)] [InlineData(2887, 2887, 2612, true)]
    [InlineData(2000, 0, 0, false)] [InlineData(2000, 0, 275, false)]
    public void Family_32_preserves_exact_viability_margin_and_signed_contrast_boundaries(int candidateWins, int gains, int losses, bool pass)
    {
        var d = Definition(); var cells = d.Teams.Select((t, member) => new TowerDiagnosticCell(t.PartyId, Enumerable.Range(0, 5500).Select(i => new TowerBalanceTrial(i,
            (member < 6 ? i < candidateWins : i >= gains && i < candidateWins || i >= candidateWins && i < candidateWins+losses) ? BattleOutcome.Victory : BattleOutcome.Draw)).ToArray())).ToArray();
        var study = new TowerFixedFamilyStudy(C.Version, new(C.Version, new string('a', 64), new string('b', 64), d), cells);
        var result = C.Assess(study, new string('c', 64));
        Assert.Equal(pass ? "RecommendFixedCandidates" : "Hold", result.Adoption);
        Assert.All(result.Contrasts, c => {
            Assert.Equal(gains, c.Gains); Assert.Equal(losses, c.Losses); Assert.Equal((gains-losses)/5500d, c.ObservedGain);
            Assert.Equal(TowerBalanceEvaluator.Wilson(gains, 5500, 32)!.Lower-
                TowerBalanceEvaluator.Wilson(losses, 5500, 32)!.Upper, c.Lower);
        });
        Assert.Equal(pass ? 6 : 0, result.QualifyingPartyIds.Count);
        Assert.All(result.Rates, r => Assert.Equal(1-.05/32, r.Estimate.Confidence));
        // Passing one anchor is insufficient; equality against the other holds adoption.
        var oneEqual = study with { Evidence = cells.Select((c, i) => i == 7 ? c with { Trials = cells[0].Trials } : c).ToArray() };
        Assert.Equal("Hold", C.Assess(oneEqual, new string('c', 64)).Adoption);
        Assert.Throws<InvalidDataException>(() => C.Assess(study with { Evidence = cells.Select((c, i) => i == 7 ? c with { Trials = c.Trials.Skip(1).ToArray() } : c).ToArray() }, new string('c', 64)));
    }

    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void Report_all_qualifiers_in_frozen_order_without_selecting_a_primary(int qualifiers)
    {
        var d = Definition();
        var cells = d.Teams.Select((t, member) => new TowerDiagnosticCell(t.PartyId, Enumerable.Range(0, 5500)
            .Select(i => new TowerBalanceTrial(i, i < (member < qualifiers ? 2500+member*1000 : 2000)
                ? BattleOutcome.Victory : BattleOutcome.Defeat)).ToArray())).ToArray();
        var study = new TowerFixedFamilyStudy(C.Version, new(C.Version, new string('a',64), new string('b',64), d), cells);
        var result = C.Assess(study, new string('c',64));
        Assert.Equal(d.Teams.Take(qualifiers).Select(t => t.PartyId), result.QualifyingPartyIds);
        Assert.Equal(d.Teams.Take(6).Select(t => t.PartyId), result.CandidateIds);
        Assert.Equal(d.Teams.Skip(6).Select(t => t.PartyId), result.ControlPartyIds);
        Assert.Equal(qualifiers == 0 ? result.ControlPartyIds : result.QualifyingPartyIds, result.RecommendedPartyIds);
        Assert.Equal(12, result.Contrasts.Count);
        for (var i = 0; i < 12; i++)
        {
            Assert.Equal(d.Teams[i/2].PartyId, result.Contrasts[i].CandidateId);
            Assert.Equal(d.Teams[6+i%2].PartyId, result.Contrasts[i].ReferenceId);
        }
        Assert.DoesNotContain("\"candidateId\":", Json(C.Export(study,result)));
        var reordered = cells.ToArray(); reordered[7] = reordered[7] with { Trials = reordered[7].Trials.Reverse().ToArray() };
        Assert.Throws<InvalidDataException>(() => C.Assess(study with { Evidence = reordered }, new string('c',64)));
        Assert.Throws<InvalidDataException>(() => C.Assess(study with { Evidence = cells.Take(7).ToArray() }, new string('c',64)));
    }

    [Theory]
    [InlineData("version")] [InlineData("party")] [InlineData("order")] [InlineData("id")] [InlineData("start")]
    [InlineData("duplicate")] [InlineData("role")] [InlineData("ability-order")]
    [InlineData("history")] [InlineData("settings")] [InlineData("phase")] [InlineData("budget")] [InlineData("overlap")]
    public void Changed_contracts_fail_before_sampling(string change)
    {
        var (q, input) = Input(); var d = input.Definition;
        if (change is "phase" or "budget" or "overlap")
        {
            q = change switch { "phase" => q with { Phases = new Dictionary<string, TowerDiagnosticPhaseLimit>() },
                "budget" => q with { PriorBytes = q.MaximumBytes-1 }, _ => q with { DefinitionPath = Path.Combine(q.OutputRoot, "source.json") } };
            Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q)); return;
        }
        var teams = d.Teams.ToArray();
        if (change == "duplicate") teams[1] = teams[0];
        if (change == "role") teams[0] = teams[0] with { Role = "PrimaryReference" };
        if (change == "ability-order") teams[0] = teams[0] with { Scenario = teams[0].Scenario with {
            Party = teams[0].Scenario.Party.Select((p, i) => i == 0 ? p with { Build = p.Build with { EssenceIds = p.Build.EssenceIds.Reverse().ToArray() } } : p).ToArray() } };
        if (change is "party" or "id" or "start") teams[0] = teams[0] with { Scenario = change switch {
            "party" => teams[0].Scenario with { Party = teams[1].Scenario.Party }, "id" => teams[0].Scenario with { Id = "changed" },
            _ => teams[0].Scenario with { StartsAt = teams[0].Scenario.StartsAt.AddSeconds(1) } } };
        d = change switch { "version" => d with { Version = "unknown" }, "order" => d with { Teams = d.Teams.Reverse().ToArray() },
            "history" => d with { ExcludedCombatSeeds = [2, 1] }, "settings" => d with { SettingsHash = new string('a', 64) }, _ => d with { Teams = teams } };
        Assert.Throws<InvalidDataException>(() => C.ValidateDefinition(d));
    }

    [Fact]
    public async Task Unknown_fields_old_commands_and_existing_outputs_are_rejected()
    {
        var (q, _) = Input(); var path = Path.Combine(root, "request.json"); HarnessJson.WriteNew(path, q);
        Edit(path, n => n["sampleExtension"] = 1); await Assert.ThrowsAsync<JsonException>(() => C.Command(["tower-fixed-family-confirmation-check", path], default));
        Edit(q.DefinitionPath, n => n["ownedCopies"] = new JsonObject()); Assert.Throws<JsonException>(() => TowerContractJson.Read<TowerFixedFamilyDefinition>(q.DefinitionPath));
        await Assert.ThrowsAnyAsync<Exception>(() => TowerPracticalSearch.Command(["tower-practical-search-check", path], default));
        await Assert.ThrowsAnyAsync<Exception>(() => TowerSelectionDiagnostic.Command(["tower-selection-diagnostic-check", path], default));
        await Assert.ThrowsAnyAsync<Exception>(() => TowerFixedTeamConfirmation.Command(["tower-fixed-team-confirmation-check", path], default));
        Directory.CreateDirectory(q.OutputRoot);
        await Assert.ThrowsAsync<InvalidDataException>(() => C.RunWithWorker(q, _ => throw new Exception("Must not launch")));
        await Assert.ThrowsAsync<InvalidDataException>(() => C.Command(["tower-fixed-family-confirmation-resume", path], default));
    }

    [Theory] [InlineData("priorSeconds")] [InlineData("priorBytes")] [InlineData("priorCharges")]
    public void Prior_cost_fields_are_required_in_serialized_requests(string field)
    {
        var (q, _) = Input(); var path = Path.Combine(root,"missing-charge.json"); HarnessJson.WriteNew(path,q);
        Edit(path, n => n.AsObject().Remove(field));
        Assert.Throws<JsonException>(() => TowerContractJson.Read<TowerFixedFamilyRequest>(path));
    }

    [Theory] [InlineData("empty")] [InlineData("duplicate")] [InlineData("sum")] [InlineData("nonfinite")] [InlineData("overlap")]
    public void Prior_receipts_must_be_unique_finite_reconciled_and_outside_output(string change)
    {
        var (q, _) = Input(); var charge = q.PriorCharges[0];
        q = q with { PriorCharges = change switch {
            "empty" => [], "duplicate" => [charge,charge], "sum" => [charge with { Bytes = charge.Bytes+1 }],
            "nonfinite" => [charge with { Seconds = double.NaN }],
            _ => [charge with { ReceiptPath = Path.Combine(q.OutputRoot,"receipt.json") }] } };
        Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q));
    }

    [Fact]
    public void Prior_receipt_changes_fail_before_entropy_and_retained_copies_bind_archive_audits()
    {
        var (q, input) = Input(); Launch(q); var freeze = C.Freeze(q,input,() => { },default);
        Assert.Equal(Json(freeze),Json(C.VerifyFreeze(q)));
        File.AppendAllText(C.P(q,"prior-charges/000.json")," ");
        Assert.Throws<InvalidDataException>(() => C.VerifyFreeze(q));
        Assert.Throws<InvalidDataException>(() => C.Reserve(q,freeze,input.History.Files,() => { },default,
            entropy: _ => throw new Exception("Changed receipt must fail before entropy")));
        Assert.False(File.Exists(C.P(q,"entropy-start.json")));
    }

    [Fact]
    public void Entropy_preserves_signed_values_collisions_duplicates_shortfall_and_tail()
    {
        var bytes = new byte[44000]; FixedFamilyFixtureHost.Entropy(bytes);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0, 4), int.MinValue);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4, 4), -987);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8, 4), int.MinValue);
        var panel = C.Classify(bytes, [-987], new string('a', 64));
        Assert.Equal(int.MinValue, panel.Panel[0]); Assert.Equal("AlreadyReserved", panel.Words[1].Classification); Assert.Equal("DuplicateBatch", panel.Words[2].Classification);
        Assert.Equal(10998, panel.NewReservations.Count); Assert.Equal(5498, panel.Words.Count(w => w.Classification == "ReservedUnused"));
        Assert.Equal(5500, panel.Panel.Count); Assert.Single(C.Classify(new byte[44000], [-987], new string('a', 64)).NewReservations);
        Assert.Throws<InvalidDataException>(() => C.Classify(bytes[..43999], [-987], new string('a', 64)));
    }

    [Theory]
    [InlineData("stage")] [InlineData("recipe")] [InlineData("id")] [InlineData("seed")]
    [InlineData("report-seed")] [InlineData("success")] [InlineData("outcome")] [InlineData("cache")]
    public async Task Invalid_or_reused_trial_stops_before_any_later_attempt(string change)
    {
        var d = Definition(); var freeze = new TowerFixedFamilyFreeze(C.Version, new string('a', 64), new string('b', 64), d);
        var starts = 0; var completed = 0; var calls = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => C.Execute(freeze, Enumerable.Range(0, 5500).ToArray(), (_, stage, scenario, seed, _) => {
            calls++; var outcome = change == "outcome" ? (BattleOutcome)999 : BattleOutcome.Victory;
            var summary = new BattleSummary(outcome, outcome, "Literal invalid fixture", 1, 1, [], [], [], new CompactCombatTelemetry());
            var report = new TowerBattleReport(new BattleReport(1, scenario.Id, change == "report-seed" ? seed+1 : seed, 1,
                JsonSerializer.SerializeToElement(new { fixture = true }), summary, null), change != "success", 0, 1);
            var trial = new LoadoutTrial(change == "id" ? "trial-999999" : $"trial-{calls:D6}", change == "stage" ? "discovery" : stage,
                change == "recipe" ? new string('0', 64) : HarnessJson.Hash(scenario), change == "seed" ? seed+1 : seed, new string('c', 64), new string('d', 64));
            return Task.FromResult((trial, report));
        }, complete => { if (complete) completed++; else starts++; }, default));
        Assert.Equal(change == "cache" ? 2 : 1, starts); Assert.Equal(starts-1, completed); Assert.Equal(starts, calls);
    }

    [Theory]
    [InlineData("entropy-pending")] [InlineData("entropy-start")] [InlineData("entropy-drawn")] [InlineData("entropy-written")]
    [InlineData("entropy-complete")] [InlineData("confirmation-binding")] [InlineData("confirmation-ledger")] [InlineData("confirmation-reserved")] [InlineData("chunks-bound")]
    public void Interruption_never_refills_reuses_or_resumes(string boundary)
    {
        var (q, input) = Input(); Launch(q); var f = C.Freeze(q, input, () => { }, default); var calls = 0;
        Assert.Throws<IOException>(() => C.Reserve(q, f, input.History.Files, () => { }, default,
            stage => { if (stage == boundary) throw new IOException("Injected interruption"); }, bytes => { calls++; FixedFamilyFixtureHost.Entropy(bytes); }));
        Assert.Equal(boundary is "entropy-pending" or "entropy-start" ? 0 : 1, calls);
        var ledger = HarnessJson.Read<JsonElement>(C.P(q, "history-input.json"));
        Assert.Equal(boundary is "confirmation-reserved" or "chunks-bound" ? "Complete" : "Pending", ledger.GetProperty("reservationState").GetString());
        Assert.False(File.Exists(C.P(q, "attempts.jsonl"))); Assert.False(File.Exists(C.P(q, "result.json")));
        Assert.Throws<IOException>(() => C.Reserve(q, f, input.History.Files, () => { }, default, entropy: _ => throw new Exception("No redraw")));
        if (boundary is not ("confirmation-reserved" or "chunks-bound")) Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.History(ledger));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Cancellation_after_draw_is_deferred_until_batch_completion_and_shortfall_is_terminal(bool cancel)
    {
        var (q, input) = Input(); Launch(q); var f = C.Freeze(q, input, () => { }, default); using var stop = new CancellationTokenSource();
        Assert.ThrowsAny<Exception>(() => C.Reserve(q, f, input.History.Files, () => { }, stop.Token,
            stage => { if (cancel && stage == "entropy-drawn") stop.Cancel(); }, _ => { }));
        Assert.True(File.Exists(C.P(q, "entropy.bin"))); Assert.True(File.Exists(C.P(q, "entropy-complete.json")));
        var history = HarnessJson.Read<JsonElement>(C.P(q, "history-input.json")); Assert.Equal(cancel ? "Pending" : "Complete", history.GetProperty("reservationState").GetString());
        if (!cancel) Assert.Equal(new[] { 0 }, TowerSearchBenchmark.History(history));
        Assert.False(File.Exists(C.P(q, "attempts.jsonl")));
    }
}
