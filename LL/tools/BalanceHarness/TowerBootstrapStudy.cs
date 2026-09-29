using System.IO.Compression;
using System.Text.Json;
using Common.Randomness;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Microsoft.Extensions.Configuration;
using Services.LL.Items;

namespace BalanceHarness;

public sealed record TowerBootstrapPanel(int Path, int Attempt, DungeonAcquisitionPanel Run);
public sealed record TowerBootstrapRequest(string Version, string ApiRoot, string Fixtures, string PlanPath, string Output,
    IReadOnlyDictionary<string, string> InputHashes, IReadOnlyList<TowerBootstrapPanel> Panels);
public sealed record TowerBootstrapStep(int Attempt, string RunFile, DungeonRunStatus Status,
    IReadOnlyList<Guid> Before, IReadOnlyList<Guid> After, FixtureEquipment? Award, double CombatSeconds);
public sealed record TowerBootstrapPath(string Cell, string Dungeon, int Path, bool Control, bool CompletedSet,
    int Attempts, int SuccessfulRuns, int FailedRuns, int SupplyItemsEarned, int StartingQuestSigils,
    int AdditionalSigilsRequired, int AssemblyOnlyAdditionalFragments, double RandomOnlyAdditionalEligibleIdleHours,
    double CombatSeconds, IReadOnlyList<TowerBootstrapStep> Steps, IReadOnlyList<FixtureEquipment> Owned,
    FixtureCharacter FinalCharacter, string Outcome);

public static class TowerBootstrapStudy
{
    public const int MaximumFights = 205824;
    public const long MaximumBytes = 256 * 1048576L;

    public static FixtureEquipment? EarnNext(TowerBootstrapCell cell, FixtureCharacter character,
        IReadOnlyList<FixtureEquipment> owned, IReadOnlyList<EquipmentSlotType> order,
        TowerEquipmentSupplyCatalog supply, TowerAcquisitionInventory comparison, string pathIdentity)
    {
        var wanted = order.Select(slot => cell.Target.Single(t => t.Slot == slot))
            .FirstOrDefault(t => !owned.Any(o => o.Slot == t.Slot && comparison.Dominates(o.Data, t.Data)));
        if (wanted is null) return null;
        var chest = supply.Candidates(1, character.Level).Single(s => s.RequiredClearedFloor == 0);
        return new(wanted.Slot, supply.Award(chest.ItemBaseId, wanted.Data.State.DefinitionId, character.Id,
            StableRandom.Guid(TowerBootstrapCohorts.Version, pathIdentity, wanted.Slot.ToString()), pathIdentity));
    }

    public static FixtureCharacter EquipRetainingStronger(FixtureCharacter character, FixtureEquipment award,
        TowerAcquisitionInventory comparison)
    {
        var previous = character.Equipment.SingleOrDefault(e => e.Slot == award.Slot);
        if (previous is not null && comparison.Dominates(previous.Data, award.Data)) return character;
        if (previous is not null && !comparison.Dominates(award.Data, previous.Data))
            throw new InvalidDataException("Incomparable gear needs an explicit equipment decision.");
        return character with { Equipment = character.Equipment.Where(e => e.Slot != award.Slot).Append(award).OrderBy(e => e.Slot).ToArray() };
    }

    public static async Task RunAsync(TowerBootstrapRequest request, CancellationToken token)
    {
        if (request.Version != TowerBootstrapCohorts.Version || Path.Exists(request.Output)) throw new InvalidDataException("Fresh bootstrap output required.");
        var plan = TowerBootstrapCohorts.Read(request.PlanPath);
        if (request.Panels.Count != 96 || request.Panels.Any(p => p.Path is < 0 or > 3 || p.Attempt is < 0 or > 11)
            || request.Panels.GroupBy(p => (p.Run.Dungeon, p.Path, p.Attempt)).Any(g => g.Count() != 1)
            || !request.Panels.Select(p => p.Run.Dungeon).Distinct().Order().SequenceEqual(new[] { "forgotten_catacombs", "goblin_mines" }))
            throw new InvalidDataException("Expected the complete predeclared source/path/attempt panel.");
        var reserved = request.Panels.SelectMany(p => p.Run.RoomSeeds.Prepend(p.Run.LayoutSeed)).ToArray();
        if (reserved.Length != 6240 || reserved.Distinct().Count() != reserved.Length) throw new InvalidDataException("Invalid reservation panel.");
        void Verify() { foreach (var p in request.InputHashes) if (HarnessJson.FileHash(p.Key) != p.Value) throw new InvalidDataException("Frozen input changed: " + p.Key); }
        Verify();
        var content = OfflineContent.ForTower(request.ApiRoot, TowerBundle.ReadSettings(request.ApiRoot));
        var cells = TowerBootstrapCohorts.Create(request.ApiRoot, request.Fixtures, plan, content);
        var supplies = JsonTowerEquipmentSupplyCatalog.Load(Path.Combine(request.ApiRoot, "Data/equipment/tower-equipment-supplies.v1.json"), content.Equipment);
        var comparison = new TowerAcquisitionInventory(content, supplies);
        var ordinary = JsonStarterEquipmentCatalog.LoadOrdinary(content.Equipment, Path.Combine(request.ApiRoot, "Data/equipment/equipment-ordinary.v1.json")).FindRegion(1)!;
        var assembly = HarnessJson.Read<JsonElement>(Path.Combine(request.ApiRoot, "Data/dungeons/sigil-assembly.json"));
        if (!assembly.GetProperty("enabled").GetBoolean()) throw new InvalidDataException("Fragment assembly is disabled.");
        var fragments = assembly.GetProperty("fragmentCost").GetInt32();
        var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(Path.Combine(request.ApiRoot, "appsettings.json"))).Build();
        var cadence = config.GetValue<int>("Combat:IdleProgression:EncounterCadenceSeconds");
        if (cadence <= 0) throw new InvalidDataException("Idle cadence missing.");
        var runner = new DungeonAcquisitionRunner(request.ApiRoot, content);
        var fights = 0; var replays = 0; var bytes = 0L;
        Directory.CreateDirectory(request.Output);
        void Save(string name, object value, bool compress = false)
        {
            var data = JsonSerializer.SerializeToUtf8Bytes(value, HarnessJson.Options);
            if (compress)
            {
                using var buffer = new MemoryStream();
                using (var gzip = new GZipStream(buffer, CompressionLevel.Fastest, leaveOpen: true)) gzip.Write(data);
                data = buffer.ToArray();
            }
            if ((bytes += data.Length) > MaximumBytes) throw new InvalidDataException("Bootstrap output cap exceeded.");
            using var file = new FileStream(Path.Combine(request.Output, name), FileMode.CreateNew);
            file.Write(data);
        }
        void Count() { token.ThrowIfCancellationRequested(); if (++fights > MaximumFights) throw new InvalidDataException("Fight bound exceeded."); }
        Save("cells.json", cells);
        Save("pre-dungeon-quests.json", TowerBootstrapCohorts.PreDungeonQuests(request.ApiRoot, plan.Level));
        var paths = new List<TowerBootstrapPath>();
        foreach (var cell in cells)
        foreach (var dungeon in new[] { "goblin_mines", "forgotten_catacombs" })
        for (var path = 0; path < plan.PathsPerSource; path++)
        {
            var isControl = cell.Gear == "rare-control";
            var character = cell.Character;
            var owned = cell.OwnedItems.ToList();
            var steps = new List<TowerBootstrapStep>();
            var acquired = 0;
            bool Covered() => cell.Target.All(t => character.Equipment.Any(e => e.Slot == t.Slot && comparison.Dominates(e.Data, t.Data)));
            for (var attempt = 0; attempt < (isControl ? 1 : plan.MaximumAttempts); attempt++)
            {
                var panel = request.Panels.Single(p => p.Run.Dungeon == dungeon && p.Path == path && p.Attempt == attempt).Run;
                var before = character.Equipment.Select(e => e.Data.State.Id).ToArray();
                var run = await runner.RunAsync(character, panel, Count, token);
                var name = $"{cell.Id}--{dungeon}--{path}--{attempt:00}.json.gz";
                Save(name, run, true);
                if (path == 0 && attempt == 0)
                {
                    var replay = await new DungeonAcquisitionRunner(request.ApiRoot, OfflineContent.ForTower(request.ApiRoot,
                        TowerBundle.ReadSettings(request.ApiRoot))).RunAsync(character, panel, Count, token);
                    Save($"{cell.Id}--{dungeon}--replay.json.gz", replay, true);
                    if (HarnessJson.Hash(run) != HarnessJson.Hash(replay)) throw new InvalidDataException("Bootstrap qualification mismatch.");
                    replays++;
                }
                FixtureEquipment? award = null;
                if (!isControl && run.Status == DungeonRunStatus.Completed)
                {
                    award = EarnNext(cell, character, owned, plan.PurchaseOrder, supplies, comparison, $"{cell.Id}/{dungeon}/{path}/{acquired + 1}")
                        ?? throw new InvalidDataException("Attempt after target inventory complete.");
                    owned.Add(award); acquired++;
                    character = EquipRetainingStronger(character, award, comparison);
                }
                steps.Add(new(attempt, name, run.Status, before, character.Equipment.Select(e => e.Data.State.Id).ToArray(), award, run.CombatSeconds));
                if (Covered()) break;
            }
            var additional = Math.Max(0, steps.Count - 1);
            var result = new TowerBootstrapPath(cell.Id, dungeon, path, isControl, !isControl && Covered(), steps.Count,
                steps.Count(s => s.Status == DungeonRunStatus.Completed), steps.Count(s => s.Status == DungeonRunStatus.Failed), acquired, 1,
                additional, additional * fragments, additional / (ordinary.SigilDropChance / ordinary.Sigils.Count) * cadence / 3600,
                steps.Sum(s => s.CombatSeconds), steps, owned, character,
                isControl ? "AlreadyOwnedRareControlNotAcquisition" : Covered() ? "EarnedCompleteSetWithinCap" : "RightCensoredAtTwelveAttempts");
            Save($"{cell.Id}--{dungeon}--{path}--path.json", result);
            paths.Add(result);
        }
        Verify();
        Save("result.json", new { version = request.Version, status = "ConditionalBootstrapDiagnosticComplete", fights, replays,
            attempts = paths.Sum(p => p.Attempts), paths = paths.Select(p => new { p.Cell, p.Dungeon, p.Path, p.Control, p.CompletedSet,
                p.Attempts, p.SuccessfulRuns, p.FailedRuns, p.SupplyItemsEarned, p.AdditionalSigilsRequired, p.CombatSeconds, p.Outcome }).ToArray(),
            measuredPlayerSamples = 0, plan.Assumptions,
            resourceInterpretation = "Conditional starting gear/training budgets and per-item drop expectations. One retained quest sigil per source; additional random-targeted idle hours assume perfect eligible idle victories and credit no other source. Fragment assembly is an alternative channel, not additional simultaneous payment. Combat seconds exclude all input, navigation, breaks and farming; no calendar estimate. Right-censored paths are not extrapolated to eventual success. Rare controls start complete and cannot prove acquisition." });
        Save("files.json", Directory.GetFiles(request.Output).Order().ToDictionary(p => Path.GetFileName(p), HarnessJson.FileHash));
    }
}
