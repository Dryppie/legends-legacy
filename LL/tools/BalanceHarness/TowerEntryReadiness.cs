using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;

namespace BalanceHarness;

public sealed record TowerEntryDecision(int Encounter, int AttemptOrdinal,
    IReadOnlyList<EquipmentSlotType> MissingSlots, string Reason, string? Dungeon);

/// <summary>A declared player-visible coverage policy, not a combat strength predictor.</summary>
public static class TowerEntryReadiness
{
    public const string Version = "tower-entry-readiness-v1";
    public static TowerSourcePolicyPlan Read(string fixtures)
    {
        var p = TowerContractJson.Read<TowerSourcePolicyPlan>(Path.Combine(fixtures, "tower-entry-readiness.json"));
        if (p.Version != Version || p.ActivityVersion != TowerActivityInventory.Version
            || !p.Policies.SequenceEqual(new[] { "mines-first-either", "full-slot-ready" })
            || p.PanelIndex != "family-attempt-ordinal" || string.IsNullOrWhiteSpace(p.Assumptions))
            throw new InvalidDataException("Changed readiness policy requires a new declaration.");
        return p;
    }

    public static IReadOnlyList<EquipmentSlotType> MissingSlots(IReadOnlyList<FixtureEquipment> equipped)
    {
        var slots = equipped.Select(e => e.Slot).ToHashSet();
        var missing = new[] { EquipmentSlotType.Head, EquipmentSlotType.Chest, EquipmentSlotType.Legs,
            EquipmentSlotType.Necklace, EquipmentSlotType.Ring, EquipmentSlotType.Relic, EquipmentSlotType.MainHand }
            .Where(s => !slots.Contains(s)).ToList();
        // A two-handed weapon covers the hand pair. Do not demand an illegal off-hand with it.
        var twoHanded = equipped.Any(e => e.Slot == EquipmentSlotType.MainHand && e.Data.EquipmentType == EquipmentType.TwoHanded);
        if (!twoHanded && !slots.Contains(EquipmentSlotType.OffHand)) missing.Add(EquipmentSlotType.OffHand);
        return missing;
    }

    public static TowerEntryDecision Decide(string policy, IReadOnlyList<FixtureEquipment> equipped,
        IReadOnlyDictionary<string, int> stock, int encounter, bool questReady, int attempts, int earned)
    {
        if (policy is not ("mines-first-either" or "full-slot-ready")) throw new InvalidDataException("Unknown readiness policy.");
        var missing = MissingSlots(equipped);
        var source = TowerActivityStudy.ChooseSource("mines-first-either", stock);
        var reason = !questReady ? "QuestGate" : earned >= 7 ? "Complete" : attempts >= 12 ? "AttemptCap"
            : policy == "full-slot-ready" && missing.Count > 0 ? "EquipmentCoverage" : source is null ? "NoSigil" : "Enter";
        return new(encounter, attempts, missing, reason, reason == "Enter" ? source : null);
    }
}
