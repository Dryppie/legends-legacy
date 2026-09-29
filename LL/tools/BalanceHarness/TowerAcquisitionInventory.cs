using Common.Randomness;
using Domain.Extensions;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Services.LL.PowerRatings;

namespace BalanceHarness;

public sealed record TowerEarnedItem(string OwnerKey, int EarnedBeforeFloor, string SupplyId,
    int SuccessfulCompletionOrdinal, EquipmentSlotType Slot, EquipmentData Data);
public sealed record TowerEarnedMember(string OwnerKey, Guid OwnerId, int NewItems,
    IReadOnlyList<Guid> EquippedItemIds, FixtureCharacter Character);

/// <summary>Personal bound inventory. Bench recipes specify demand, never donated items.</summary>
public sealed class TowerAcquisitionInventory(OfflineContent content, TowerEquipmentSupplyCatalog supplies)
{
    private readonly List<TowerEarnedItem> _items = [];
    public IReadOnlyList<TowerEarnedItem> Items => _items;

    public TowerEarnedMember Equip(string ownerKey, int floor, EquipmentReferenceBuildDefinition requested,
        IReadOnlyList<TowerEquipmentSupply> available)
    {
        if (string.IsNullOrWhiteSpace(ownerKey) || floor < 1 || available.Count == 0)
            throw new InvalidDataException("An owner and an eligible supply source are required.");
        var owner = StableRandom.Guid("tower-acquisition-owner-v1", ownerKey);
        var reference = FixtureCharacter.From(content.CreateBuild(requested));
        var equipped = new List<FixtureEquipment>();
        var earned = 0;
        foreach (var selection in reference.Equipment)
        {
            var wanted = selection.Data;
            // Same archetype/specialization only: no scalar power claim across different builds.
            var existing = _items.Where(i => i.OwnerKey == ownerKey && i.Slot == selection.Slot
                    && Dominates(i.Data, wanted) && CanEquip(i.Data, requested.CharacterLevel))
                .OrderByDescending(i => i.Data.State.Tier).ThenByDescending(i => i.Data.Rarity)
                .ThenByDescending(i => i.Data.Quality).ThenByDescending(i => i.Data.State.Rank).FirstOrDefault();
            if (existing is null)
            {
                var definition = content.Equipment.Evaluator.GetDefinition(wanted.State.DefinitionId);
                // Prefer the strongest eligible plain supply, including an older region after floor 10.
                var choices = available.SelectMany(s => supplies.Choices(s.ItemBaseId)
                    .Where(c => content.Equipment.Evaluator.GetDefinition(c.Id) is var d
                        && d.ArchetypeId == definition.ArchetypeId && d.SpecializationId == definition.SpecializationId)
                    .Select(c => (Supply: s, Choice: c)))
                    .OrderByDescending(c => c.Supply.Tier).ThenByDescending(c => c.Supply.Band.Rarity)
                    .ThenByDescending(c => c.Supply.Band.Quality).ThenByDescending(c => c.Supply.Band.Rank);
                foreach (var choice in choices)
                {
                    var ordinal = _items.Count(i => i.OwnerKey == ownerKey) + 1;
                    var itemId = StableRandom.Guid("tower-acquisition-item-v1", ownerKey, ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    var data = supplies.Award(choice.Supply.ItemBaseId, choice.Choice.Id, owner, itemId,
                        $"acquisition/{ownerKey}/{ordinal}");
                    if (!Dominates(data, wanted) || !CanEquip(data, requested.CharacterLevel)) continue;
                    existing = new(ownerKey, floor, choice.Supply.ItemBaseId, ordinal, selection.Slot, data);
                    _items.Add(existing);
                    earned++;
                    break;
                }
            }
            if (existing is null) throw new InvalidDataException($"No earned route for {ownerKey}/{selection.Slot} before floor {floor}.");
            if (existing.Data.State.Ownership.OwnerId != owner)
                throw new InvalidDataException("Bound equipment cannot move between modeled owners.");
            equipped.Add(new(selection.Slot, existing.Data));
        }
        EquipmentReferenceBuildFactory.ValidateEquipmentSlots(equipped.Select(e => (e.Slot, e.Data.EquipmentType)).ToArray());
        return new(ownerKey, owner, earned, equipped.Select(e => e.Data.State.Id).ToArray(),
            reference with { Id = owner, Name = ownerKey, Equipment = equipped });
    }

    public bool Dominates(EquipmentData owned, EquipmentData wanted)
    {
        var a = content.Equipment.Evaluator.GetDefinition(owned.State.DefinitionId);
        var b = content.Equipment.Evaluator.GetDefinition(wanted.State.DefinitionId);
        return a.ArchetypeId == b.ArchetypeId && a.SpecializationId == b.SpecializationId
            && owned.State.ActiveStyleId == wanted.State.ActiveStyleId
            && owned.State.Tier >= wanted.State.Tier && owned.State.Rank >= wanted.State.Rank
            && owned.Rarity >= wanted.Rarity && owned.Quality >= wanted.Quality
            && owned.AttributeRollMultiplier >= wanted.AttributeRollMultiplier
            && wanted.Stats.All(s => owned.Stats.GetValueOrDefault(s.Key) >= s.Value);
    }

    public static object CombatDescriptor(EquipmentData data) => new
    {
        data.ItemBaseId, data.Rarity, data.EquipmentType, data.Quality, data.AttributeRollMultiplier,
        data.State.DefinitionId, data.State.Tier, data.State.Rank, data.State.ActiveStyleId,
        data.State.BalanceVersion, data.Behavior, data.Stats, data.BaseStats, data.EquipmentSetId, data.Allocation
    };

    public static long DismantleParts(TowerEarnedItem item, EquipmentUpgradePrices prices) =>
        checked(prices.GetDismantleParts(item.Data.State.Tier, item.Data.State.Rank) * item.Data.EquipmentType.OccupiedSlotCount());

    private static bool CanEquip(EquipmentData data, int level) =>
        level >= EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(data.State.Tier);
}
