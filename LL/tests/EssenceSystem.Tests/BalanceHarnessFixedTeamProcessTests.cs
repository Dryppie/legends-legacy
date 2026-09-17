using System.Diagnostics;
using System.Text.Json;
using BalanceHarness;
using BalanceHarness.ProcessFixture;
using C = BalanceHarness.TowerFixedTeamConfirmation;

namespace EssenceSystem.Tests;

public sealed partial class BalanceHarnessFixedTeamConfirmationTests
{
    [Theory]
    [InlineData("negative")] [InlineData("entropy-pending-hang")] [InlineData("storage-hang")]
    public async Task Owned_process_completes_negative_evidence_and_stops_at_phase_and_storage_caps(string mode)
    {
        var (q, _) = Input();
        if (mode == "entropy-pending-hang") q = q with { Phases = q.Phases.ToDictionary(p => p.Key, p => p.Key == "admission" ? p.Value with { Seconds = 5 } : p.Value) };
        var fixture = Path.Combine(root, "process.json"); HarnessJson.WriteNew(fixture, new FixedTeamFixture(FixedTeamFixtureHost.Version, mode, q, Settings));
        var start = FixtureHost.Start("fixed-team-parent", fixture); start.RedirectStandardOutput = true; start.RedirectStandardError = true;
        using var process = Process.Start(start)!; var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(550)); }
        finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); } }
        var text = await stdout; var error = await stderr; Assert.True(mode == "negative" ? process.ExitCode == 0 : process.ExitCode != 0, text+error);
        Assert.Equal(mode == "negative", File.Exists(C.P(q, "closeout.json")));
        if (mode == "negative")
        {
            var result = await C.VerifyPublication(q.OutputRoot, ct => FixedTeamFixtureHost.Verify(q, ct));
            Assert.Equal("Hold", result.Adoption); Assert.Equal("StrengthNotDemonstrated", result.StrengthDecision);
            Assert.Equal(result.ControlPartyIds, result.RecommendedPartyIds); Assert.All(result.Contrasts, c => Assert.Equal(0, c.ObservedGain));
            output.WriteLine("Owned negative literal fixture: "+File.ReadAllText(C.P(q, "closeout.json")));
        }
        else
        {
            Assert.Equal("Pending", HarnessJson.Read<JsonElement>(C.P(q, "history-input.json")).GetProperty("reservationState").GetString());
            await Assert.ThrowsAnyAsync<Exception>(() => C.Verify(q.OutputRoot));
        }
    }

    [Theory] [InlineData("parent")] [InlineData("worker")]
    public async Task Owner_or_worker_death_keeps_blocking_pending_without_publication(string killed)
    {
        var (q, _) = Input(); var fixture = Path.Combine(root, "process.json");
        HarnessJson.WriteNew(fixture, new FixedTeamFixture(FixedTeamFixtureHost.Version, "entropy-pending-hang", q, Settings));
        var start = FixtureHost.Start("fixed-team-parent", fixture); start.RedirectStandardOutput = true; start.RedirectStandardError = true;
        using var parent = Process.Start(start)!; var stdout = parent.StandardOutput.ReadToEndAsync(); var stderr = parent.StandardError.ReadToEndAsync(); Process? worker = null;
        try
        {
            await Ready(q, parent);
            var receipt = HarnessJson.Read<TowerPracticalWorkerStart>(C.P(q, "worker-start.json")); worker = Process.GetProcessById(receipt.ProcessId);
            Assert.Equal(receipt.ProcessStartedUtcTicks, worker.StartTime.ToUniversalTime().Ticks);
            if (killed == "parent") parent.Kill(); else worker.Kill();
            await parent.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(File.Exists(C.P(q, "closeout.json"))); Assert.False(File.Exists(C.P(q, "entropy.bin")));
            Assert.Equal("Pending", HarnessJson.Read<JsonElement>(C.P(q, "history-input.json")).GetProperty("reservationState").GetString());
        }
        finally
        {
            if (worker is not null) { if (!worker.HasExited) worker.Kill(entireProcessTree: true); await worker.WaitForExitAsync(); worker.Dispose(); }
            if (!parent.HasExited) parent.Kill(entireProcessTree: true); await parent.WaitForExitAsync(); await stdout; await stderr;
        }
    }

    private static async Task Ready(TowerFixedTeamRequest q, Process? parent = null)
    {
        var clock = Stopwatch.StartNew(); var marker = C.P(q, "fixture-ready.json");
        while (!File.Exists(marker) && parent?.HasExited != true && clock.Elapsed < TimeSpan.FromSeconds(15)) await Task.Delay(50);
        Assert.True(File.Exists(marker), "Worker did not reach its owned boundary.");
    }

    [Fact]
    public async Task Cancellation_kills_started_attempt_and_registry_lease_prevents_a_second_writer()
    {
        var (q, _) = Input();
        using (TowerCompactBundle.AcquireWriter(Path.Combine(q.RegistryRoot, "complete-family-allocation")))
            await Assert.ThrowsAnyAsync<Exception>(() => C.RunWithWorker(q, _ => throw new Exception("Second writer must not launch")));
        Assert.False(Path.Exists(q.OutputRoot));
        var fixture = Path.Combine(root, "process.json"); HarnessJson.WriteNew(fixture, new FixedTeamFixture(FixedTeamFixtureHost.Version, "attempt-hang", q, Settings));
        using var stop = new CancellationTokenSource(); var operation = C.RunWithWorker(q, _ => FixtureHost.Start("fixed-team-worker", fixture), stop.Token);
        try
        {
            await Ready(q); var receipt = HarnessJson.Read<TowerPracticalWorkerStart>(C.P(q, "worker-start.json")); using var worker = Process.GetProcessById(receipt.ProcessId);
            stop.Cancel(); var result = await operation.WaitAsync(TimeSpan.FromSeconds(15)); await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("IncompleteEvidence", result.StrengthDecision); Assert.Equal("Failed", result.IntegrityStatus); Assert.Empty(result.RecommendedPartyIds);
            Assert.Single(File.ReadLines(C.P(q, "attempts.jsonl"))); Assert.False(File.Exists(C.P(q, "result.json")));
            Assert.Equal(11000, TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(C.P(q, "history-input.json"))).Length);
            await Assert.ThrowsAsync<InvalidDataException>(() => C.RunWithWorker(q, _ => throw new Exception("No resume")));
        }
        finally { stop.Cancel(); await operation; }
    }

    [Fact]
    public void Cumulative_and_nontransferable_resource_limits_include_prior_charges()
    {
        var (q, _) = Input(); q = q with { MaximumSeconds = 2460, PriorSeconds = 1860, MaximumBytes = 1344L*1048576, PriorBytes = 1088L*1048576 };
        C.ValidateRequest(q); C.CheckEnvelope(q, 0, 0);
        Assert.Throws<InvalidDataException>(() => C.CheckEnvelope(q, 598, 0));
        Assert.Throws<InvalidDataException>(() => C.CheckEnvelope(q, 0, 252L*1048576+1));
        var launch = Launch(q); var phase = new TowerDiagnosticPhase("admission", launch.StartedAt, launch.StartedAt.AddSeconds(60), 0);
        HarnessJson.WriteNew(C.P(q, "phase.json"), phase);
        Assert.Throws<InvalidDataException>(() => C.CheckActivePhase(q, 32L*1048576+1));
        Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q with { Phases = q.Phases.ToDictionary(p => p.Key,
            p => p.Key == "audit" ? p.Value with { Bytes = 65L*1048576 } : p.Value) }));
    }

    [Theory]
    [InlineData("before-publication")] [InlineData("result-published")] [InlineData("teams-published")]
    [InlineData("before-seal")] [InlineData("after-seal")] [InlineData("closeout-published")]
    public async Task Every_publication_interruption_is_failed_even_if_result_or_closeout_was_written(string boundary)
    {
        var (q, input) = Input();
        // Unit fixture for the parent's publication boundary. The complete report workflow is tested separately.
        var result = await C.RunWithWorker(q, _ => {
            var freeze = C.Freeze(q, input, () => { }, default);
            var study = new TowerFixedTeamStudy(C.Version, freeze, input.Definition.Teams.Select(t => new TowerDiagnosticCell(t.PartyId,
                Enumerable.Range(0, 5500).Select(i => new TowerBalanceTrial(i, Domain.Models.Combat.BattleOutcome.Defeat)).ToArray())).ToArray());
            Directory.CreateDirectory(C.P(q, "study")); HarnessJson.WriteNew(C.P(q, "study/study.json"), study); C.Seal(C.P(q, "study"));
            var expected = C.Assess(study, HarnessJson.FileHash(C.P(q, "study/files.json")));
            HarnessJson.WriteNew(C.P(q, "worker-result.json"), expected); HarnessJson.WriteNew(C.P(q, "independent-audit.json"), expected);
            HarnessJson.WriteNew(C.P(q, "native-audit.json"), new { status = "Passed", expected.StudyHash, expected.ArchiveHash, newFights = 0 });
            HarnessJson.WriteNew(C.P(q, "proposed-teams.json"), C.Export(study, expected));
            var now = DateTimeOffset.UtcNow;
            HarnessJson.WriteNew(C.P(q, "phase.json"), new TowerDiagnosticPhase("audit", now, now.AddSeconds(q.Phases["audit"].Seconds), TowerBulkCampaign.StorageBytes(q.OutputRoot, default)));
            return new ProcessStartInfo("dotnet", "--version") { UseShellExecute = false, CreateNoWindow = true };
        }, boundary: stage => { if (stage == boundary) throw new IOException("Injected publication interruption"); });
        Assert.Equal("Injected publication interruption", result.StopReason); Assert.Equal("Failed", result.IntegrityStatus);
        Assert.True(File.Exists(C.P(q, "failure.json"))); await Assert.ThrowsAsync<InvalidDataException>(() => C.Verify(q.OutputRoot));
        Assert.Empty(result.RecommendedPartyIds);
        await Assert.ThrowsAsync<InvalidDataException>(() => C.RunWithWorker(q, _ => throw new Exception("No publication resume")));
    }

    [Fact]
    public void Legacy_recovery_cannot_unblock_this_entropy_contract()
    {
        var (q, input) = Input(); Launch(q); var freeze = C.Freeze(q, input, () => { }, default);
        Assert.Throws<IOException>(() => C.Reserve(q, freeze, input.History.Files, () => { }, default,
            stage => { if (stage == "entropy-start") throw new IOException("Interrupted"); }, _ => throw new Exception("No entropy")));
        var manifest = Path.Combine(root, "recovery-manifest.json");
        HarnessJson.WriteNew(manifest, Directory.GetFiles(q.OutputRoot).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
        var before = Directory.GetFiles(q.OutputRoot).ToDictionary(p => p, HarnessJson.FileHash);
        foreach (var version in new[] { TowerPracticalReservationRecovery.Version, TowerPracticalReservationRecovery.AllocationVersion })
        {
            var request = new TowerPracticalRecoveryRequest(version, q.OutputRoot, manifest, HarnessJson.FileHash(manifest), Path.Combine(root, "receipt.json"), 10);
            Assert.ThrowsAny<Exception>(() => TowerPracticalReservationRecovery.Recover(request)); Assert.False(File.Exists(request.ReceiptPath));
        }
        Assert.All(before, p => Assert.Equal(p.Value, HarnessJson.FileHash(p.Key)));
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Refresh(q.RegistryRoot, Path.Combine(root, "next"), input.History.Files, [-987], default));
    }
}
