using System.Text.Json;

namespace BalanceHarness;

public sealed partial class TowerDashboardService
{
    private (IReadOnlyList<int> Seeds, IReadOnlyList<string> Sources) StudySeedExclusions()
    {
        var seeds = TowerPartyProgression.HistoricalSeeds.ToHashSet();
        var sources = new List<string> { "Built-in historical Tower schedules" };
        // Always import these historical seeds, independently of whether the party is a reference.
        var userFile = Path.Combine(catalogsRoot, "tower-floor-1-user-party.json");
        foreach (var seed in HarnessJson.Read<JsonElement>(userFile).GetProperty("seeds").EnumerateArray()) seeds.Add(seed.GetInt32());
        sources.Add("User benchmark seeds: " + HarnessJson.FileHash(userFile));
        foreach (var study in TowerRetainedBuilds.Load(catalogsRoot, _runsRoot))
        {
            seeds.UnionWith(study.CombatSeeds);
            sources.Add("Retained build history: " + study.Id + ": " + study.EvidenceHash);
        }
        var pending = new Queue<(string Path, int Depth)>();
        if (Directory.Exists(_runsRoot)) pending.Enqueue((_runsRoot, 0));
        var visited = 0;
        void Integers(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value)) seeds.Add(value);
            else if (element.ValueKind == JsonValueKind.Array) foreach (var child in element.EnumerateArray()) Integers(child);
            else if (element.ValueKind == JsonValueKind.Object) foreach (var property in element.EnumerateObject()) Integers(property.Value);
        }
        while (pending.Count > 0)
        {
            if (++visited > 4000) throw new InvalidDataException("Historical seed scan exceeds 4,000 directories. Use a scoped results folder and import the additional exclusions explicitly.");
            var (path, depth) = pending.Dequeue();
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) continue;
            var covered = false;
            foreach (var name in new[] { "seed-ledger.json", "excluded-seeds.json" })
            {
                var file = Path.Combine(path, name); if (!File.Exists(file)) continue;
                Integers(HarnessJson.Read<JsonElement>(file)); sources.Add(Path.GetRelativePath(_runsRoot, file) + ": " + HarnessJson.FileHash(file));
                covered |= name == "seed-ledger.json";
            }
            var definitionPath = Path.Combine(path, "definition.json");
            if (File.Exists(definitionPath))
            {
                var element = HarnessJson.Read<JsonElement>(definitionPath);
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("stages", out _))
                {
                    var d = TowerBossDiscovery.Read(definitionPath); TowerBossDiscovery.Validate(d);
                    seeds.UnionWith(d.ExcludedCombatSeeds);
                    seeds.UnionWith(d.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection).Concat(s.Confirmation).Concat(s.Diagnostics))); covered = true;
                }
                else if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("objective", out _) && element.TryGetProperty("controls", out _))
                {
                    var d = TowerBossSearch.Read(definitionPath); seeds.UnionWith(d.ExcludedCombatSeeds); seeds.UnionWith(TowerBossSearch.CombatSeeds(d)); covered = true;
                }
                else if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("characterSeed", out _))
                {
                    var d = TowerPartySearch.Read(definitionPath); seeds.UnionWith(d.ExcludedCombatSeeds); seeds.UnionWith(TowerPartyProgression.CombatSeeds(d)); covered = true;
                }
                if (covered) sources.Add(Path.GetRelativePath(_runsRoot, definitionPath) + ": " + HarnessJson.FileHash(definitionPath));
            }
            var ledger = Path.Combine(path, "trials.jsonl");
            if (File.Exists(ledger))
            {
                foreach (var line in File.ReadLines(ledger)) seeds.Add(JsonSerializer.Deserialize<LoadoutTrial>(line, HarnessJson.Options)!.Seed);
                sources.Add(Path.GetRelativePath(_runsRoot, ledger) + ": " + HarnessJson.FileHash(ledger)); covered = true;
            }
            var tower = Path.Combine(path, "tower-input.json");
            if (File.Exists(tower))
            {
                var input = HarnessJson.Read<JsonElement>(tower);
                void TowerSeeds(JsonElement value)
                {
                    if (value.ValueKind == JsonValueKind.Array) foreach (var child in value.EnumerateArray()) TowerSeeds(child);
                    else if (value.ValueKind == JsonValueKind.Object)
                    {
                        if (value.TryGetProperty("scenario", out var scenario)) TowerSeeds(scenario);
                        if (value.TryGetProperty("seeds", out var schedule)) Integers(schedule);
                        if (value.TryGetProperty("rules", out var rules) && rules.TryGetProperty("randomSeed", out var seed)) Integers(seed);
                    }
                }
                TowerSeeds(input);
                sources.Add(Path.GetRelativePath(_runsRoot, tower) + ": " + HarnessJson.FileHash(tower)); covered = true;
            }
            if (covered || depth >= 4) continue;
            foreach (var child in Directory.EnumerateDirectories(path).Order(StringComparer.Ordinal))
                if (Path.GetFileName(child) is not ("content" or "source" or "source-snapshot" or "executable" or "verification-executable" or "verified-source" or "replays" or "recipes" or "battles" or ".git"))
                    pending.Enqueue((child, depth + 1));
        }
        if (seeds.Count > 100000) throw new InvalidDataException("Historical exclusion union exceeds the study contract limit.");
        return (seeds.Order().ToArray(), sources);
    }
}
