using System.Diagnostics;
using System.Text.Json;
using BalanceHarness;

namespace BalanceHarness.ProcessFixture;

public sealed record ProcessFixture(string Version, string Mode, TowerPracticalRequest Request,
    BossStudyReport Report, TowerBossInventoryReport Inventory);

// A separate test executable, never shipped by or selectable through the harness.
// Ordinary fixtures consume fabricated reports. The separate resource probe may
// prepare native inputs and read retained reports, but never execute combat.
public static class FixtureHost
{
    public const string Version = "synthetic-practical-process-fixture-v1";
    public static int AllocationCandidate(string stage, int ordinal) => stage switch {
        "construction" => 17 + ordinal, "discovery" => 101 + ordinal,
        "selection" => 201 + ordinal, "confirmation" => 301 + ordinal,
        _ => throw new InvalidDataException("Unknown literal fixture stage.")
    };
    public static ProcessStartInfo Start(string command, string fixturePath)
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
        // The fixture is a project reference of the test assembly. Its dependency
        // graph and framework settings are available beside the copied executable.
        foreach (var argument in new[] { "exec", "--runtimeconfig", Path.Combine(AppContext.BaseDirectory, "EssenceSystem.Tests.runtimeconfig.json"),
            "--depsfile", Path.Combine(AppContext.BaseDirectory, "EssenceSystem.Tests.deps.json"), typeof(FixtureHost).Assembly.Location, command, fixturePath })
            start.ArgumentList.Add(argument);
        return start;
    }

    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length != 2) throw new InvalidDataException("Use this test host with a generated process fixture.");
            if (args[0].StartsWith("resource-probe-", StringComparison.Ordinal)) return await ResourceProbeHost.Command(args[0], args[1]);
            if (args[0].StartsWith("diagnostic-", StringComparison.Ordinal)) return await DiagnosticFixtureHost.Run(args[0], args[1]);
            if (args[0].StartsWith("fixed-team-", StringComparison.Ordinal)) return await FixedTeamFixtureHost.Run(args[0], args[1]);
            var fixture = HarnessJson.Read<ProcessFixture>(args[1]); var q = fixture.Request;
            if (fixture.Version != Version || !Path.GetFileName(q.RegistryRoot).StartsWith("tower-practical-process-fixture-", StringComparison.Ordinal)
                || !string.Equals(Path.GetDirectoryName(q.RegistryRoot), Path.TrimEndingDirectorySeparator(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Only isolated temporary synthetic fixtures are supported.");
            var source = TowerBossDiscovery.Read(q.DefinitionPath);
            var definition = q.Allocation is null ? TowerPracticalSearch.Prepare(source) : source;
            if (q.Allocation is not null) TowerPracticalSearch.ValidateAllocationTemplate(q, definition);
            if (definition.ContentHashes.Values.Any(h => h != new string('a', 64)) || definition.SettingsHash != new string('b', 64)
                || definition.ExecutionHash != new string('c', 64) || definition.Contexts.Single().Id != "fixture")
                throw new InvalidDataException("Real gameplay bindings are forbidden in the process fixture.");
            using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Process fixture entered combat.")).Activate();
            switch (args[0])
            {
                case "parent":
                    var result = await TowerPracticalSearch.RunWithWorker(q, _ => Start("worker", args[1]), allocationCandidate: AllocationCandidate);
                    Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options));
                    return result.IntegrityStatus == "Verified" ? 0 : result.ExecutionStatus == "Cancelled" ? 130 : 2;
                case "worker":
                    return await TowerPracticalSearch.WorkerWithOperation(q.OutputRoot, (request, launch, token) =>
                        TowerPracticalSearch.RunOperation(request, launch, ct => {
                            ct.ThrowIfCancellationRequested();
                            var history = TowerRefinementComparisonLaunch.Refresh(q.RegistryRoot, q.OutputRoot, q.RequiredHistory,
                                definition.ExcludedCombatSeeds.Order().ToArray(), ct);
                            return new(definition, history);
                        }, (d, study, attempt, ct) => {
                            var count = fixture.Report.Accounting.Attempted.Values.Sum();
                            for (var i = 0; i < count; i++)
                            {
                                attempt(false);
                                if (fixture.Mode == "attempt-hang") Block(q, "attempt");
                                attempt(true);
                            }
                            Directory.CreateDirectory(study);
                            HarnessJson.WriteNew(Path.Combine(study, "definition.json"), d);
                            HarnessJson.WriteNew(Path.Combine(study, "study.json"), fixture.Report);
                            HarnessJson.WriteNew(Path.Combine(study, "boss-profiles.json"), fixture.Inventory);
                            HarnessJson.WriteNew(Path.Combine(study, "files.json"), Directory.EnumerateFiles(study)
                                .ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
                            return Task.FromResult(fixture.Report);
                        }, (_, _) => Task.FromResult(fixture.Report), token, stage => {
                            if (fixture.Mode == stage + "-hang") Block(q, stage);
                            if (stage is not ("pending" or "allocation-pending")) return;
                            if (fixture.Mode == "storage-hang")
                            {
                                Block(q, "storage", () => {
                                    using var file = new FileStream(Path.Combine(q.OutputRoot, "fixture-overflow.bin"), FileMode.CreateNew);
                                    file.SetLength(q.MaximumBytes);
                                });
                            }
                            if (fixture.Mode is "pending-hang" or "wrong-parent") Block(q, "pending");
                        }, AllocationCandidate), default);
                case "owner":
                    // Keep the owner alive but omit its monitoring loop to isolate
                    // the real worker watchdog's absolute deadline and identity guard.
                    using (var parent = Process.GetCurrentProcess())
                    {
                        Directory.CreateDirectory(q.OutputRoot);
                        var now = DateTimeOffset.UtcNow;
                        var launch = new TowerPracticalLaunch(HarnessJson.Hash(q), now, now.AddSeconds(q.MaximumSeconds - q.PriorSeconds),
                            parent.Id, parent.StartTime.ToUniversalTime().Ticks - (fixture.Mode == "wrong-parent" ? 1 : 0));
                        HarnessJson.WriteNew(Path.Combine(q.OutputRoot, "request.json"), q);
                        HarnessJson.WriteNew(Path.Combine(q.OutputRoot, "launch.json"), launch);
                        var start = Start("worker", args[1]); start.RedirectStandardOutput = true; start.RedirectStandardError = true;
                        using var worker = Process.Start(start)!;
                        var stdout = worker.StandardOutput.ReadToEndAsync(); var stderr = worker.StandardError.ReadToEndAsync();
                        await worker.WaitForExitAsync();
                        await Console.Out.WriteAsync(await stdout); await Console.Error.WriteAsync(await stderr);
                        return worker.ExitCode;
                    }
                case "verify":
                    await TowerPracticalSearch.VerifyPublication(q.OutputRoot, (_, _) => Task.FromResult(fixture.Report), allocationCandidate: AllocationCandidate);
                    return 0;
                default: throw new InvalidDataException("Unknown process-fixture command.");
            }
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 2; }
    }

    private static void Block(TowerPracticalRequest q, string stage, Action? afterAttach = null)
    {
        var marker = Path.Combine(q.RegistryRoot, "fixture-blocked.json");
        HarnessJson.WriteNew(marker + ".pending", new { stage }); File.Move(marker + ".pending", marker);
        if (afterAttach is not null)
        {
            // Let the test retain the worker handle before the storage monitor
            // can terminate it. This handshake is outside the run's evidence.
            while (!File.Exists(marker + ".continue")) Thread.Sleep(10);
            afterAttach();
        }
        Thread.Sleep(Timeout.Infinite); // Deliberately ignores cancellation; only the real process controls can stop it.
    }
}
