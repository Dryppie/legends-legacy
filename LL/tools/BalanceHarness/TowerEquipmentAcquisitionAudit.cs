using Domain.Extensions;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Items;

namespace BalanceHarness;

public sealed record TowerEquipmentAcquisitionRow(int Characters, int Items, int OccupiedSlots, int Tier,
    int MissingOrdinaryDefinitions, double ChampionLegendaryMasterpiecePerEquipmentDrop,
    double ChampionLegendaryMasterpiecePerCompletionAtMasteryZero,
    double ChampionLegendaryMasterpiecePerCompletionAtMasteryTen,
    double ExpectedDropsIgnoringFitAndRolls, TowerReinforcementCost ReinforcementFromDungeonRank,
    string Scope);

/// <summary>Static ordinary-drop eligibility and reinforcement costs; does not grant gear or infer play time.</summary>
public static class TowerEquipmentAcquisitionAudit
{
    public static TowerEquipmentAcquisitionRow Inspect(TowerScenario scenario, OfflineContent content,
        CombatAcquisitionCatalog acquisition, EquipmentUpgradePrices prices)
    {
        var party = scenario.Party.Select(p => content.CreateBuild(p.Build)).ToArray();
        var items = party.SelectMany(p => p.Equipment).Select(e => e.ProgressionData!).ToArray();
        if (items.Length == 0 || items.Any(e => e.Rarity != EquipmentRarity.Legendary
            || e.State.Quality != ItemQuality.Masterpiece || e.State.Rank != 5)
            || items.Select(e => e.State.Tier).Distinct().Count() != 1)
            throw new InvalidDataException("This acquisition audit requires a complete Legendary/Masterpiece/rank-5 budget at one tier.");
        var tier = items[0].State.Tier;
        var pool = acquisition.Pools.Single(p => p.EquipmentTier == tier);
        var allowed = acquisition.BaseDropDefinitions(EquipmentRarity.Legendary).Select(d => d.Id).ToHashSet(StringComparer.Ordinal);
        var missing = scenario.Party.SelectMany(p => p.Build.Equipment).Count(e => !allowed.Contains(e.DefinitionId));
        var chance = pool.DungeonEquipment.Rarities.Champion.Legendary * pool.DungeonEquipment.Qualities.Masterpiece;
        if (chance <= 0) throw new InvalidDataException("The declared gear has no ordinary Champion drop path.");
        return new(party.Length, items.Length, items.Sum(e => e.EquipmentType.OccupiedSlotCount()), tier, missing, chance,
            chance * pool.DungeonEquipment.DropChanceAtMastery(0), chance * pool.DungeonEquipment.DropChanceAtMastery(10),
            items.Length / chance, TowerProgressionPreview.ReinforcementCost(party, prices, pool.DungeonEquipment.Rank),
            "Ordinary Champion equipment-drop channel only. Expected drops counts any Legendary/Masterpiece item as useful, ignoring archetype, specialization, style, roll, duplicates and ownership; not a loadout acquisition forecast or play-time estimate. Other reward/trading channels, sigils, clears and material income are not modeled. Baseline reference rolls are not a guaranteed award.");
    }
}
