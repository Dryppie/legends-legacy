using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

// All labels are literals, outside the production registry. No default allocator or combat engine.
internal sealed class BalanceHarnessRefinementLaunchFixture
{
    internal string Root { get; } = Path.Combine(Environment.GetEnvironmentVariable("TOWER_REFINEMENT_FIXTURE_ROOT") ?? Path.GetTempPath(),
        "tower-refinement-launch-" + Guid.NewGuid().ToString("N"));
    internal TowerRefinementLaunchRequest Request { get; }
    internal TowerRefinementLaunchAuthorization Permit { get; }
    internal TowerRefinementLaunchInputs Inputs { get; }
    internal BalanceHarnessRefinementLaunchFixture(string? archiveProfile = null, string? comparisonVersion = null)
    {
        Directory.CreateDirectory(Root);
        var ledger = Path.Combine(Root, "seed-ledger.json"); HarnessJson.WriteNew(ledger, new { reservationState = "Complete", reserved = new[] { -1 } });
        var preflight = new TowerRefinementPreflightRequest(Root, ledger, ledger, ledger, ledger, ledger, 1,
            new Dictionary<string, string> { [ledger] = HarnessJson.FileHash(ledger) }, comparisonVersion);
        Request = new(preflight, Root, Path.Combine(Root, "study"), 7, 10, 60, 16 * 1048576, new string('a', 64), HarnessJson.Hash(ExecutionIdentity.Current()), archiveProfile);
        Permit = new(HarnessJson.Hash(Request), Request.ProtocolHash, 45, 288, 0);
        var result = new TowerRefinementPreflightResult("BoundAwaitingAuthorization", false, TowerRefinementComparisonModel.ResolveVersion(comparisonVersion), 1,
            HarnessJson.Hash(new[] { -1 }), "synthetic-fixture", true, 45, 288, new Dictionary<string, string>(), new Dictionary<string, string>(), new string('a', 64), ExecutionIdentity.Current(), HarnessJson.Hash(preflight));
        Inputs = new(result, BalanceHarnessRefinementComparisonFixture.Definitions()[0],
            TowerRefinementComparisonLaunch.Refresh(Root, Request.StudyRoot, preflight.Pins, [-1], default));
    }
    internal string P(string n) => Path.Combine(Request.StudyRoot, n);
    internal static int Candidate(string stage, int ordinal) => stage switch {
        "generation" => 17 + ordinal, "discovery" => 101 + ordinal, "selection" => 201 + ordinal, "confirmation" => 301 + ordinal,
        _ => throw new InvalidDataException("Unexpected fixture stage.") };
    internal void Bind(CancellationToken token = default, Func<string, int, int>? candidate = null, Action<string>? boundary = null,
        Action<IReadOnlyDictionary<string, string>, CancellationToken>? recheck = null)
        => TowerRefinementComparisonLaunch.BindCore(Request, Permit, _ => Inputs,
            recheck ?? ((files, ct) => TowerRefinementComparisonLaunch.Recheck(Root, Request.StudyRoot, files, ct)), token, candidate ?? Candidate, boundary);
    internal Task<TowerRefinementComparisonQuality> Run(Func<CancellationToken, TowerRefinementLaunchInputs>? inspect = null,
        Func<string, TowerBossDiscoveryDefinition[], int, long, CancellationToken, Task<TowerRefinementComparisonQuality>>? execute = null,
        Func<string, CancellationToken, Task<TowerRefinementComparisonQuality>>? verify = null)
        => TowerRefinementComparisonLaunch.RunCore(Request, Permit, inspect ?? (_ => Inputs),
            execute ?? ((p, d, s, b, ct) => TowerRefinementComparisonRun.Execute(p, d, s, b, new BalanceHarnessRefinementComparisonFixture(), ct, Request.ArchiveProfile)),
            verify ?? ((p, ct) => TowerRefinementComparisonRun.Reconstruct(p, new BalanceHarnessRefinementComparisonFixture(), ct)), default, Candidate);
    internal object Summary() => new { root = Root, registeredHistory = 1, syntheticLabels = 45,
        histories = HarnessJson.Read<JsonElement>(P("history-input.json")), binding = HarnessJson.Read<JsonElement>(P("binding.json")),
        fights = 0, newSeeds = 0, runtimePreparations = 0 };
}
