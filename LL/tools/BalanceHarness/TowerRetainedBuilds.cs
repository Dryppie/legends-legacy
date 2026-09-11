namespace BalanceHarness;

public sealed record RetainedTowerBuild(string Id, string RecipeHash, TowerScenario Scenario);
public sealed record RetainedTowerStudy(string Id, string EvidenceHash, TowerSearchBudget Budget,
    int RequiredPartySize, IReadOnlyList<int> CombatSeeds, IReadOnlyList<RetainedTowerBuild> Builds);
public sealed record RetainedTowerCatalog(int SchemaVersion, IReadOnlyList<RetainedTowerStudy> Studies);

/// <summary>Persistent exact controls. Historical outcomes never enter independent generation.</summary>
public static class TowerRetainedBuilds
{
    public const string FixtureFile = "tower-retained-builds.json";
    public const string LocalFile = "retained-tower-builds.json";

    public static RetainedTowerCatalog Read(string path)
    {
        if (!File.Exists(path)) return new(1, []);
        var catalog = TowerContractJson.Read<RetainedTowerCatalog>(path);
        if (catalog.SchemaVersion != 1 || catalog.Studies is null || catalog.Studies.Count > 10000
            || catalog.Studies.Any(s => s is null || !TowerBenchmark.SafeId(s.Id)
                || !TowerContractJson.Hash(s.EvidenceHash) || !TowerBossDiscovery.LegalBudget(s.Budget)
                || s.RequiredPartySize is < 1 or > 50 || s.CombatSeeds is null || s.CombatSeeds.Count > 100000
                || s.CombatSeeds.Distinct().Count() != s.CombatSeeds.Count || s.Builds is not { Count: > 0 and <= 20 })
            || catalog.Studies.Select(s => s.Id).Distinct().Count() != catalog.Studies.Count)
            throw new InvalidDataException("Invalid retained Tower benchmark catalog.");
        foreach (var study in catalog.Studies)
        foreach (var build in study.Builds)
        {
            if (build is null || !TowerBenchmark.SafeId(build.Id) || build.Scenario is null
                || build.Scenario.SchemaVersion != 1 || build.Scenario.FloorNumber != study.Budget.PriorityFloor
                || build.Scenario.PreparationState != "uncleared-no-contributions" || build.Scenario.Seeds is not { Count: 0 }
                || HarnessJson.Hash(build.Scenario) != build.RecipeHash)
                throw new InvalidDataException("A retained Tower recipe changed or has invalid provenance/scope.");
            TowerBossDiscovery.ValidateEquipment(build.Scenario.Party, study.Budget, study.RequiredPartySize);
        }
        return catalog;
    }

    public static IReadOnlyList<RetainedTowerStudy> Load(string catalogsRoot, string runsRoot) =>
        Read(Path.Combine(catalogsRoot, FixtureFile)).Studies.Concat(Read(Path.Combine(runsRoot, LocalFile)).Studies).ToArray();

    public static IReadOnlyList<BossBenchmarkReference> Compatible(TowerBossDiscoveryDefinition d,
        IReadOnlyList<RetainedTowerStudy> studies)
    {
        var result = new List<BossBenchmarkReference>(); var seen = new HashSet<string>();
        foreach (var study in studies.Where(s => s.Budget == d.Budget && s.RequiredPartySize == d.RequiredPartySize))
        foreach (var build in study.Builds)
        {
            if (build.Scenario.StartsAt != d.StartsAt) continue;
            var context = d.Contexts.SingleOrDefault(c => TowerBossDiscovery.EquipmentBudgetHash(c.CharacterTemplates)
                == TowerBossDiscovery.EquipmentBudgetHash(build.Scenario.Party));
            if (context is null) continue;
            try
            {
                // A reduced Essence pool or owned-copy budget may legitimately exclude an old recipe.
                TowerBossDiscovery.ValidateParty(d, TowerPartySelection.Choice("retained-reference",
                    build.Scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds)));
            }
            catch (InvalidDataException) { continue; }
            var key = HarnessJson.Hash(new { Context = context.Id, Recipe = TowerBossDiscovery.RecipeHash(build.Scenario.Party) });
            if (!seen.Add(key)) continue;
            result.Add(new("retained-" + key[..40], context.Id, build.Scenario,
                $"Saved benchmark build from {study.Id}; remeasure on fresh seeds, reference only", study.EvidenceHash));
        }
        return result;
    }

    public static void Remember(string runsRoot, string run, TowerBossDiscoveryDefinition d, BossStudyReport report)
    {
        // Failed balance is still valuable search evidence. Partial/cancelled studies never become controls.
        if (report.Status != "Complete" || report.Confirmation is null) return;
        var cells = report.Confirmation.Members.Where(m => m.GeneratedIds.Count > 0).Select(m => m.CellId).ToHashSet();
        var builds = report.Confirmation.Definition.Cells.Where(c => cells.Contains(c.Id)).Select(c =>
        {
            var scenario = c.Scenario with { Seeds = [] };
            return new RetainedTowerBuild(c.Id, HarnessJson.Hash(scenario), scenario);
        }).ToArray();
        if (builds.Length == 0) return;
        var evidence = HarnessJson.FileHash(Path.Combine(run, "study.json"));
        var study = new RetainedTowerStudy("study-" + evidence[..40], evidence, d.Budget, d.RequiredPartySize,
            d.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection).Concat(s.Confirmation).Concat(s.Diagnostics)).Distinct().Order().ToArray(), builds);
        Directory.CreateDirectory(runsRoot);
        var path = Path.Combine(runsRoot, LocalFile);
        using var guard = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var catalog = Read(path);
        if (catalog.Studies.Any(s => s.Id == study.Id)) return;
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            HarnessJson.WriteNew(temporary, catalog with { Studies = [.. catalog.Studies, study] });
            Read(temporary);
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
