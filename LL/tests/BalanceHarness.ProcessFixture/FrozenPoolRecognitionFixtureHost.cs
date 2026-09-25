using System.Text.Json;
using C = BalanceHarness.TowerFixedFamilyConfirmation;

namespace BalanceHarness.ProcessFixture;

// Literal reports through the actual owned worker, both auditors and final publication barrier.
public static class FrozenPoolRecognitionFixtureHost
{
    public static async Task<int> Run(string command, string path, string version = C.RecognitionVersion)
    {
        var affinity = version == C.AffinityRecognitionVersion;
        var preservation = version == C.PreservationRecognitionVersion;
        var neighborhood = version == C.NeighborhoodRecognitionVersion;
        var prefix = neighborhood ? "neighborhood-recognition-fixture-" : preservation ? "preservation-recognition-fixture-" : affinity ? "affinity-recognition-fixture-" : "recognition-fixture-";
        var registryPrefix = neighborhood ? "tower-neighborhood-recognition-owned-fixture-" : preservation ? "tower-preservation-recognition-owned-fixture-" : affinity ? "tower-affinity-recognition-owned-fixture-" : "tower-recognition-owned-fixture-";
        var mode = neighborhood ? "neighborhood-recognition-owned" : preservation ? "preservation-recognition-owned" : affinity ? "affinity-recognition-owned" : "recognition-owned";
        var fixture = TowerContractJson.Read<FixedFamilyFixture>(path); var q = fixture.Request;
        if (!C.IsRecognition(version) || !command.StartsWith(prefix,StringComparison.Ordinal)
            || fixture.Version != FixedFamilyFixtureHost.Version || fixture.Mode != mode
            || !Path.GetFileName(q.RegistryRoot).StartsWith(registryPrefix,StringComparison.Ordinal)
            || Directory.EnumerateFileSystemEntries(q.ContentRoot).Any() || q.Version != version)
            throw new InvalidDataException("Require a separately named synthetic fixture with an empty content root.");
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Owned fixture entered combat.")).Activate();
        var operations = new TowerRecognitionOperations((request,ct) => {
            var d = TowerContractJson.Read<TowerFixedFamilyDefinition>(request.DefinitionPath);
            return new(d,TowerRefinementComparisonLaunch.Refresh(request.RegistryRoot,request.OutputRoot,request.RequiredHistory,d.ExcludedCombatSeeds.ToArray(),ct));
        }, (request,freeze,check,ct) => FixedFamilyFixtureHost.Prepare(request,freeze,fixture.Settings,check,ct),
            (request,freeze,panel,attempt,check,ct) => FixedFamilyFixtureHost.Study(request,freeze,panel,"complete",attempt,check,ct),FixedFamilyFixtureHost.Entropy);
        object result = command[prefix.Length..] switch {
            "run" => await C.RunRecognitionOwned(q.OutputRoot,default,operations),
            "audit" => await C.AuditRecognition(q.OutputRoot,default,FixedFamilyFixtureHost.Input),
            "publication-check" => C.RecognitionPublicationCheck(q.OutputRoot,default),
            "verify" => await C.VerifyRecognition(q.OutputRoot,default,FixedFamilyFixtureHost.Input),
            _ => throw new InvalidDataException("Unknown recognition fixture command.")
        };
        Console.WriteLine(JsonSerializer.Serialize(result,HarnessJson.Options)); return 0;
    }
}
