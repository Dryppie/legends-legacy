using System.Text.Json;
using Domain.Extensions;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Items;
using Services.LL.PowerRatings;

namespace BalanceHarness;

public sealed record TowerProgressionDraft(string Version, string EquipmentStatus, string Assumptions,
    IReadOnlyList<TowerSearchBudget> Budgets);
public sealed record TowerReinforcementCost(int StartingRank, long Parts, long Cinders);
public sealed record TowerProgressionPreviewRow(TowerSearchBudget Budget, int Characters, int Items,
    int OccupiedEquipmentSlots, TowerReinforcementCost FromRankZero, TowerReinforcementCost? FromRankOne,
    IReadOnlyList<TowerProgressionPreviewParty> Parties);
public sealed record TowerProgressionPreviewParty(string GearProfile, TowerScenario Scenario);
public sealed record TowerProgressionPreviewReport(string Version, string Status, TowerProgressionDraft Draft,
    TowerSettings Settings, ExecutionIdentity Execution, IReadOnlyDictionary<string, string> SourceHashes,
    IReadOnlyList<TowerProgressionPreviewRow> Floors, int Fights, int ReservedSeeds);

/// <summary>Prepares explicit progression assumptions for review; never runs or accepts a balance study.</summary>
public static class TowerProgressionPreview
{
    public const string Version = "tower-progression-budget-preview-v1";
    public const string Fixture = "tower-progression-budget-draft.json";
    private const string PricesFile = "equipment/equipment-upgrades.v1.json";

    public static void Validate(TowerProgressionDraft draft)
    {
        if (draft.Version != Version || draft.EquipmentStatus != "Provisional"
            || string.IsNullOrWhiteSpace(draft.Assumptions) || draft.Budgets is not { Count: 11 }
            || draft.Budgets.Any(b => b is null || !TowerBossDiscovery.LegalBudget(b)
                || !TowerBossDiscovery.LegalPurpose(b, "intended-progression")
                || b.CharacterLevel < EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(b.Tier))
            || !draft.Budgets.Select(b => b.PriorityFloor).SequenceEqual(Enumerable.Range(1, 11)))
            throw new InvalidDataException("Declare provisional legal budgets for floors 1–11 in order, preserving the approved Essence checkpoints.");
    }

    public static TowerReinforcementCost ReinforcementCost(IReadOnlyList<EquipmentReferenceBuild> party,
        EquipmentUpgradePrices prices, int startingRank)
    {
        if (startingRank < 0 || party.Any(p => p.Definition.Rank < startingRank))
            throw new InvalidDataException("Reinforcement costs cannot include rank downgrades.");
        long parts = 0, cinders = 0;
        foreach (var item in party.SelectMany(p => p.Equipment))
        {
            var data = item.ProgressionData!;
            var tier = prices.ForTier(data.State.Tier);
            var slots = data.EquipmentType.OccupiedSlotCount();
            for (var rank = startingRank; rank < data.State.Rank; rank++)
            {
                parts = checked(parts + tier.RankPartCosts[rank] * slots);
                cinders = checked(cinders + tier.RankCinderCosts[rank] * slots);
            }
        }
        return new(startingRank, parts, cinders);
    }

    public static async Task<TowerProgressionPreviewReport> CreateAsync(string draftPath, string root,
        string catalogs, string gearPath, CancellationToken token = default)
    {
        // Detect source drift across preparation. This is a preview, not a sealed combat archive.
        var paths = new SortedDictionary<string, string>(StringComparer.Ordinal) {
            ["draft"] = draftPath, ["gearProfiles"] = gearPath,
            ["authoredCurve"] = Path.Combine(catalogs, "tower-curve.json"),
            ["settings"] = Path.Combine(root, "appsettings.json"),
            [PricesFile] = Path.Combine(root, "Data", PricesFile)
        };
        foreach (var file in TowerBundle.Files) paths[file] = Path.Combine(root, "Data", file);
        var hashes = paths.ToDictionary(p => p.Key, p => HarnessJson.FileHash(p.Value));
        var draft = TowerContractJson.Read<TowerProgressionDraft>(draftPath);
        Validate(draft);
        var profiles = TowerGearProfiles.Read(gearPath);
        var settings = TowerBundle.ReadSettings(root);
        var content = OfflineContent.ForTower(root, settings);
        var runner = new TowerBattleRunner(root, content);
        var prices = JsonEquipmentUpgradePrices.Load(paths[PricesFile]);
        var rows = new List<TowerProgressionPreviewRow>();
        foreach (var budget in draft.Budgets)
        {
            token.ThrowIfCancellationRequested();
            var authored = TowerPartyProgression.Scenarios(root, catalogs, budget)
                .Single(s => s.FloorNumber == budget.PriorityFloor) with { Seeds = [] };
            authored = authored with { Assumptions = [.. authored.Assumptions,
                draft.Assumptions, "Budget preview only. Authored composition; retained specialists and search results must be added before balance evaluation. No strength or acceptance claim."] };
            var variants = new[] { new TowerProgressionPreviewParty("baseline", authored) }
                .Concat(profiles.Profiles.Select(p => new TowerProgressionPreviewParty(p.Id, TowerGearProfiles.Apply(authored, p, content)))).ToArray();
            foreach (var variant in variants)
            {
                TowerBossDiscovery.ValidateEquipment(variant.Scenario.Party, budget, authored.Party.Count);
                // Required only by the preparation API. No combat uses or reserves this placeholder.
                var input = runner.CreateInput(variant.Scenario with { Seeds = [1] }, 1,
                    settings.Threat, settings.CheckpointIntervalTicks);
                _ = await runner.PrepareAsync(input, token);
            }
            var party = authored.Party.Select(p => content.CreateBuild(p.Build)).ToArray();
            if (party.SelectMany(p => p.Equipment).Any(e => e.ProgressionData!.Rarity != EquipmentRarity.Uncommon)
                || party.Any(p => p.Definition.AttributeRollMultiplier != 1))
                throw new InvalidDataException("The draft assumes Uncommon equipment with baseline rolls.");
            rows.Add(new(budget, party.Length, party.Sum(p => p.Equipment.Count),
                party.Sum(p => p.Equipment.Sum(e => e.ProgressionData!.EquipmentType.OccupiedSlotCount())),
                ReinforcementCost(party, prices, 0), budget.Rank >= 1 ? ReinforcementCost(party, prices, 1) : null,
                variants));
        }
        foreach (var path in paths)
            if (hashes[path.Key] != HarnessJson.FileHash(path.Value))
                throw new InvalidDataException("A preview source changed during preparation; no result was exported.");
        return new(Version, "PreparedDraftNotBalanceEvidence", draft, settings, ExecutionIdentity.Current(), hashes, rows, 0, 0);
    }

    public static async Task<int> Command(string[] args, CancellationToken token)
    {
        if (args is not ["tower-progression-budget-preview", var draft, var root, var catalogs, var gear, var output])
            throw new InvalidDataException("Use tower-progression-budget-preview <draft.json> <content-root> <fixtures-root> <gear-profiles.json> <new-preview.json>. Zero combat.");
        if (Path.Exists(output)) throw new IOException("Choose a new preview file.");
        var report = await CreateAsync(draft, root, catalogs, gear, token);
        HarnessJson.WriteNew(output, report);
        Console.WriteLine(JsonSerializer.Serialize(new { report.Status, floors = report.Floors.Count,
            parties = report.Floors.Sum(r => r.Parties.Count), report.Fights, report.ReservedSeeds }, HarnessJson.Options));
        return 0;
    }
}
