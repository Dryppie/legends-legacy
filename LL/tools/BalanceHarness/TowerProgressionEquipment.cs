using System.Text.Json;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;

namespace BalanceHarness;

public sealed record TowerProgressionEquipmentBand(int FirstFloor, int LastFloor,
    EquipmentRarity Rarity, ItemQuality Quality, int Rank);
public sealed record TowerProgressionEquipmentCycle(int Length, IReadOnlyList<TowerProgressionEquipmentBand> Bands);

/// <summary>Repeating equipment assumptions, independent of level, tier and Essence progression.</summary>
public static class TowerProgressionEquipment
{
    public static void Validate(TowerProgressionEquipmentCycle cycle)
    {
        if (cycle is null || cycle.Length != 10 || cycle.Bands is not { Count: > 0 and <= 10 }
            || cycle.Bands.Any(b => b is null || b.FirstFloor < 1 || b.LastFloor > cycle.Length
                || b.LastFloor < b.FirstFloor || !Enum.IsDefined(b.Rarity) || b.Rarity == EquipmentRarity.Legacy
                || !Enum.IsDefined(b.Quality) || b.Rank is < 0 or > EquipmentBalance.MaximumRank)
            || !cycle.Bands.SelectMany(b => Enumerable.Range(b.FirstFloor, b.LastFloor - b.FirstFloor + 1))
                .SequenceEqual(Enumerable.Range(1, cycle.Length)))
            throw new InvalidDataException("Equipment bands must cover positions 1–10 exactly once in order with legal rarity, quality and rank.");
    }

    public static TowerProgressionEquipmentBand ForFloor(TowerProgressionEquipmentCycle cycle, int floor)
    {
        Validate(cycle);
        if (floor < 1) throw new ArgumentOutOfRangeException(nameof(floor));
        var position = (floor - 1) % cycle.Length + 1;
        return cycle.Bands.Single(b => b.FirstFloor <= position && position <= b.LastFloor);
    }

    public static TowerScenario Apply(TowerScenario scenario, TowerProgressionEquipmentCycle cycle, OfflineContent content)
    {
        var band = ForFloor(cycle, scenario.FloorNumber);
        var copy = TowerBatchRacing.Copy(scenario);
        var evaluator = content.Equipment.Evaluator;
        var party = copy.Party.Select(member => {
            var build = member.Build;
            if (member.Doctrine is not null || build.Equipment.Any(e => e.UseNativeStyle || e.ActiveStyleId is not null))
                throw new InvalidDataException("This equipment progression uses unstyled reference builds.");
            var selections = build.Equipment.Select(item => {
                var original = evaluator.GetDefinition(item.DefinitionId);
                var matches = evaluator.Definitions.Where(d => d.ArchetypeId == original.ArchetypeId
                    && d.SpecializationId == original.SpecializationId && d.Rarity == band.Rarity
                    && d.NativeStyleId is null).ToArray();
                if (matches.Length != 1)
                    throw new InvalidDataException($"Expected one {band.Rarity} definition for {original.ArchetypeId}/{original.SpecializationId}.");
                return item with { DefinitionId = matches[0].Id };
            }).ToArray();
            var changed = build with { Equipment = selections, Rank = band.Rank, Quality = band.Quality,
                IdentityEquipment = build.IdentityEquipment ?? build.Equipment };
            _ = content.CreateBuild(changed);
            return member with { Build = changed };
        }).ToArray();
        return copy with { Party = party, Seeds = [], Assumptions = [
            $"Equipment at cycle position {(scenario.FloorNumber - 1) % cycle.Length + 1}: {band.Rarity}, {band.Quality}, rank {band.Rank}. Level, tier, rolls and ordered Essences retain their source values. Hypothetical ownership; no styles.",
            "New equipment budget: prior strength claims do not transfer. Seed-free input; requires a separately declared evaluation schedule.",
            .. copy.Assumptions.Select(a => "Source assumption (prior budget): " + a)] };
    }

    public static async Task<int> Command(string[] args, CancellationToken token)
    {
        if (args is not ["tower-progression-gear-apply", var scenarioPath, var budgetPath, var root, var output])
            throw new InvalidDataException("Use tower-progression-gear-apply <scenario.json> <cycle-budget.json> <content-root> <new-scenario.json>. Zero combat.");
        if (Path.Exists(output)) throw new IOException("Choose a new scenario file.");
        var draft = TowerContractJson.Read<TowerProgressionDraft>(budgetPath);
        TowerProgressionPreview.Validate(draft);
        if (draft.EquipmentCycle is null) throw new InvalidDataException("A repeating equipment cycle is required.");
        var settings = TowerBundle.ReadSettings(root);
        var content = OfflineContent.ForTower(root, settings);
        var scenario = Apply(TowerContractJson.Read<TowerScenario>(scenarioPath), draft.EquipmentCycle, content);
        var runner = new TowerBattleRunner(root, content);
        _ = await runner.PrepareAsync(runner.CreateInput(scenario with { Seeds = [1] }, 1,
            settings.Threat, settings.CheckpointIntervalTicks), token);
        HarnessJson.WriteNew(output, scenario);
        Console.WriteLine(JsonSerializer.Serialize(new { status = "ProgressionEquipmentApplied", scenario.FloorNumber,
            equipment = ForFloor(draft.EquipmentCycle, scenario.FloorNumber), fights = 0, reservedSeeds = 0 }, HarnessJson.Options));
        return 0;
    }
}
