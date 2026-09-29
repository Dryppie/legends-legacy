using System.Text.Json;
using Domain.Models.Dungeons.Runs;

namespace BalanceHarness;

public sealed record DungeonAcquisitionCell(string Id, string Assumptions, FixtureCharacter Character);
public sealed record DungeonAcquisitionRequest(string Version, string ApiRoot, string Fixtures, string AcquisitionReport,
    string Output, IReadOnlyDictionary<string, string> InputHashes, IReadOnlyList<DungeonAcquisitionPanel> Panels);

/// <summary>A small fixed diagnostic, not a Tower search or a player-time estimate.</summary>
public static class DungeonAcquisitionStudy
{
    public const string Version = "tower-dungeon-acquisition-qualification-v1";
    public const int Samples = 8;
    public const int MaximumFights = 30000;
    public const long MaximumBytes = 256 * 1048576L;
    public const string AcquisitionPin = "6df2d6196bf51a024860d34e00a5cb4b347398de3378a8dbcd2222a2a3e63dc5";

    public static IReadOnlyList<DungeonAcquisitionCell> Cells(string root, string fixtures, string acquisitionReport)
    {
        if (HarnessJson.FileHash(acquisitionReport) != AcquisitionPin) throw new InvalidDataException("Changed earned inventory receipt.");
        var content = OfflineContent.ForTower(root, TowerBundle.ReadSettings(root));
        var rows = HarnessJson.Read<JsonElement>(acquisitionReport).GetProperty("floors").Deserialize<TowerAcquisitionFloor[]>(HarnessJson.Options)!;
        var result = new List<DungeonAcquisitionCell>();
        var starters = HarnessJson.Read<IdleSuiteDefinition>(Path.Combine(fixtures, "idle-first-hunt.json"))
            .Stages.Single(s => s.Id == "first-hunt").Builds;
        foreach (var build in starters)
        {
            var character = FixtureCharacter.From(content.CreateBuild(build with { CharacterLevel = 30 }));
            result.Add(new("starter-" + build.Id,
                "Level 30 supplied; one First Hunt weapon and one untrained starter Essence. No armor, training or extra drops. Conservative bootstrap control, not a typical level-30 inventory.", character));
        }
        foreach (var (floor, prior) in new[] { (1, 1), (4, 3), (7, 6), (10, 9), (11, 10) })
        foreach (var slot in new[] { 1, 2, 3, 5 })
        {
            var next = rows.Single(r => r.Floor == floor).Members.Single(m => m.OwnerKey == $"cohort-{slot:00}");
            var owned = rows.Single(r => r.Floor == prior).Members.Single(m => m.OwnerKey == next.OwnerKey);
            var character = next.Character with { Equipment = owned.Character.Equipment };
            result.Add(new($"{(floor == 1 ? "post" : "pre")}-floor-{floor:00}-slot-{slot}",
                floor == 1 ? "Positive equipment control: full first Rare supply set ALREADY owned. Not bootstrap evidence. Authored four-Essence recipe supplied."
                : $"Level and ordered Essences at floor {floor} supplied; exact personally owned equipment from floor {prior}. No target-band upgrades during these attempts.", character));
        }
        if (result.Count != 26 || result.Select(c => c.Id).Distinct().Count() != 26) throw new InvalidDataException("Changed fixed cohort.");
        return result;
    }

    public static async Task RunAsync(DungeonAcquisitionRequest request, CancellationToken token)
    {
        if (request.Version != Version || Path.Exists(request.Output) || request.Panels.Count != 16
            || request.Panels.GroupBy(p => p.Dungeon).Any(g => g.Count() != Samples)
            || !request.Panels.Select(p => p.Dungeon).Distinct().Order().SequenceEqual(new[] { "forgotten_catacombs", "goblin_mines" }))
            throw new InvalidDataException("Use a fresh bounded eight-layout, two-source qualification.");
        var reserved = request.Panels.SelectMany(p => p.RoomSeeds.Prepend(p.LayoutSeed)).ToArray();
        if (reserved.Length != 1040 || reserved.Distinct().Count() != reserved.Length) throw new InvalidDataException("Overlapping reservations.");
        void VerifyInputs()
        {
            foreach (var pair in request.InputHashes)
                if (HarnessJson.FileHash(pair.Key) != pair.Value) throw new InvalidDataException("Frozen input changed: " + pair.Key);
        }
        VerifyInputs();
        var cells = Cells(request.ApiRoot, request.Fixtures, request.AcquisitionReport);
        Directory.CreateDirectory(request.Output);
        var bytes = 0L;
        void Write(string name, object value)
        {
            var path = Path.Combine(request.Output, name);
            var data = JsonSerializer.SerializeToUtf8Bytes(value, HarnessJson.Options);
            if ((bytes += data.Length) > MaximumBytes) throw new InvalidDataException("Output cap exceeded.");
            using var stream = new FileStream(path, FileMode.CreateNew);
            stream.Write(data);
        }
        Write("cells.json", cells);
        var content = OfflineContent.ForTower(request.ApiRoot, TowerBundle.ReadSettings(request.ApiRoot));
        var runner = new DungeonAcquisitionRunner(request.ApiRoot, content);
        var fights = 0;
        var replays = 0;
        var index = new List<object>();
        var scores = new List<object>();
        void CountFight() { token.ThrowIfCancellationRequested(); if (++fights > MaximumFights) throw new InvalidDataException("Fight cap exceeded."); }
        foreach (var cell in cells)
        foreach (var family in request.Panels.GroupBy(p => p.Dungeon))
        {
            var runs = new List<DungeonAcquisitionRun>();
            var sample = 0;
            foreach (var panel in family)
            {
                var run = await runner.RunAsync(cell.Character, panel, CountFight, token);
                var file = $"{cell.Id}--{panel.Dungeon}--{sample:00}.json";
                Write(file, run); // retain completed work even if the next qualification fails
                index.Add(new { cell = cell.Id, panel.Dungeon, sample, file, run.Status, fights = run.Battles.Count });
                if (sample == 0)
                {
                    // Replay each cell through a newly composed production path, including complete summaries/preparation.
                    var replay = await new DungeonAcquisitionRunner(request.ApiRoot, OfflineContent.ForTower(request.ApiRoot,
                        TowerBundle.ReadSettings(request.ApiRoot))).RunAsync(cell.Character, panel, CountFight, token);
                    Write($"{cell.Id}--{panel.Dungeon}--replay.json", replay);
                    if (HarnessJson.Hash(run) != HarnessJson.Hash(replay)) throw new InvalidDataException("Full-run qualification replay differs.");
                    replays++;
                }
                runs.Add(run);
                sample++;
            }
            var wins = runs.Where(r => r.Status == DungeonRunStatus.Completed).ToArray();
            var losses = runs.Except(wins).ToArray();
            scores.Add(new { cell = cell.Id, dungeon = family.Key, successes = wins.Length, attempts = runs.Count,
                failures = losses.Length, sigilsConsumed = runs.Sum(r => r.SigilsConsumed),
                successCombatSeconds = wins.Select(r => r.CombatSeconds).ToArray(),
                failureCombatSeconds = losses.Select(r => r.CombatSeconds).ToArray(),
                observedProbability = wins.Length / (double)runs.Count,
                // Do not extrapolate infinity or an unsupported expected farming time from eight samples.
                sevenItemAttempts = (double?)null, measuredPlayerSamples = 0 });
        }
        VerifyInputs();
        Write("result.json", new { version = Version, status = "DiagnosticCompleteNotPaceAcceptance", fights, replays,
            routePolicy = DungeonAcquisitionRunner.RoutePolicy, masteryAtEntry = 0, scores, index,
            limitations = new[] { "One character per dungeon; no shared Tower-party clears.",
                "Sigils are charged from supplied starting stock, not earned inside this combat study.",
                "Rewards/guild/mastery persistence excluded; completion callbacks recorded. Supply issuance has separate production service tests.",
                "Snapshot fixed across rooms as in production. No post-win upgrades, mastery accumulation or leveling between attempts.",
                "Deterministic slot IDs, encounter IDs, seeds and timestamps replace nondeterministic orchestration identity only.",
                "Simulation combat seconds omit input, animations, navigation, waiting, sigil farming and offline time.",
                "Eight layouts per source are diagnostics, not balance acceptance or population success estimates.",
                "No search, recipe selection, boss changes, economy changes, retries or adaptive sample extension." } });
        var hashes = Directory.GetFiles(request.Output).Order().ToDictionary(Path.GetFileName, HarnessJson.FileHash);
        Write("files.json", hashes);
    }
}
