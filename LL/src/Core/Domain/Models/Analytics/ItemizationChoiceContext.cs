using Domain.Models.Attributes;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;

namespace Domain.Models.Analytics;

/// <summary>A single-item replacement opportunity, not a claim that all alternatives can be worn together.</summary>
public sealed record ItemizationChoiceContext(int CharacterLevel, bool Complete, bool CandidateEligible,
    string? CandidateIneligibility, IReadOnlyList<ItemizationAlternative> Alternatives, int UnversionedItems)
{
    public ItemizationAlternative? Candidate { get; init; }
    public int EligibleAlternatives => Alternatives.Count(x => x.Eligible);
}

public sealed record ItemizationAlternative(Guid ItemId, EquipmentType Type, int Tier, int StatVersion,
    string? Specialization, bool Equipped, bool Eligible, string? Ineligibility,
    IReadOnlyList<EquipmentSlotType> Slots, IReadOnlyDictionary<AttributeType, float> Stats);

public interface IItemizationChoiceRepository
{
    Task<ItemizationChoiceInventory?> CaptureAsync(Guid characterId, CancellationToken ct);
}

/// <summary>Frozen before a committed decision. Listed items and unborrowed vault items are absent.</summary>
public sealed record ItemizationChoiceInventory(Guid CharacterId, int Level,
    IReadOnlyList<EquipmentSlotType> Slots, IReadOnlyList<ItemizationAlternative> Items, int UnversionedItems)
{
    public static ItemizationChoiceInventory Capture(Guid characterId, int level,
        IReadOnlyList<EquipmentSlotType> slots, IEnumerable<(EquipmentInstance Item, bool Equipped)> items,
        IReadOnlySet<Guid> validGuildLoans)
    {
        var unique = items.GroupBy(x => x.Item.Id).Select(g => (g.First().Item, Equipped: g.Any(x => x.Equipped))).ToArray();
        return new(characterId, level, slots, unique.Where(x => x.Item.ProgressionData is not null)
            .Select(x => Describe(characterId, level, slots, x.Item.ProgressionData!, x.Equipped,
                validGuildLoans.Contains(x.Item.Id))).OrderBy(x => x.ItemId).ToArray(),
            unique.Count(x => x.Item.ProgressionData is null));
    }

    public ItemizationChoiceInventory WithAwards(IEnumerable<EquipmentData> awards) => this with
    {
        Items = Items.Concat(awards.Select(x => Describe(CharacterId, Level, Slots, x, false, false)))
            .DistinctBy(x => x.ItemId).OrderBy(x => x.ItemId).ToArray()
    };

    public ItemizationChoiceContext? ForItem(Guid itemId, EquipmentSlotType? slot = null)
    {
        var candidate = Items.SingleOrDefault(x => x.ItemId == itemId);
        if (candidate is null) return null;
        var targets = slot.HasValue ? new[] { slot.Value } : candidate.Slots;
        var validTarget = targets.Any(candidate.Slots.Contains);
        return new(Level, UnversionedItems == 0, candidate.Eligible && validTarget,
            candidate.Ineligibility ?? (validTarget ? null : "incompatible-slot"),
            Items.Where(x => x.ItemId != itemId && x.Slots.Intersect(targets).Any()).ToArray(), UnversionedItems) { Candidate = candidate };
    }

    private static ItemizationAlternative Describe(Guid characterId, int level,
        IReadOnlyList<EquipmentSlotType> availableSlots, EquipmentData item, bool equipped, bool validGuildLoan)
    {
        EquipmentSlotType[] slots = item.EquipmentType switch
        {
            EquipmentType.OneHanded or EquipmentType.TwoHanded => [EquipmentSlotType.MainHand, EquipmentSlotType.OffHand],
            EquipmentType.OffHand => [EquipmentSlotType.OffHand],
            _ => [Enum.Parse<EquipmentSlotType>(item.EquipmentType.ToString())]
        };
        var ownership = item.State.Ownership;
        var reason = ownership.Kind == EquipmentOwnershipKind.GuildOwned
            ? (validGuildLoan ? null : "invalid-guild-loan")
            : ownership.OwnerId == characterId ? null : "different-owner";
        if (level < EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(item.State.Tier)) reason ??= "level";
        // The equip repository requires both hand slots even for one-handed items.
        var requiredSlots = item.EquipmentType is EquipmentType.OneHanded or EquipmentType.TwoHanded or EquipmentType.OffHand
            ? new[] { EquipmentSlotType.MainHand, EquipmentSlotType.OffHand } : slots;
        if (requiredSlots.Any(x => !availableSlots.Contains(x))) reason ??= "missing-slot";
        return new(item.State.Id, item.EquipmentType, item.State.Tier, item.StatVersion,
            item.Allocation?.SpecializationId, equipped, reason is null, reason, slots, item.Stats);
    }
}
