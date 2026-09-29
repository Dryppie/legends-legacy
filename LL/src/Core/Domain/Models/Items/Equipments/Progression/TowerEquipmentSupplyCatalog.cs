namespace Domain.Models.Items.Equipments.Progression;

public sealed record TowerEquipmentBand(EquipmentRarity Rarity, ItemQuality Quality, int Rank)
{
    public static TowerEquipmentBand ForFloor(int floor)
    {
        if (floor < 1) throw new ArgumentOutOfRangeException(nameof(floor));
        return ((floor - 1) % 10 + 1) switch
        {
            <= 3 => new(EquipmentRarity.Rare, ItemQuality.Standard, 2),
            <= 6 => new(EquipmentRarity.Epic, ItemQuality.Fine, 3),
            <= 9 => new(EquipmentRarity.Unique, ItemQuality.Exceptional, 4),
            _ => new(EquipmentRarity.Legendary, ItemQuality.Masterpiece, 5)
        };
    }
}

// Tier, source region and level are authored separately from the repeating equipment band.
public sealed record TowerEquipmentSupply(string ItemBaseId, string Name, int TargetFloor,
    int SourceRegion, int Tier, int MinimumLevel)
{
    public int RequiredClearedFloor => TargetFloor - 1;
    public TowerEquipmentBand Band => TowerEquipmentBand.ForFloor(TargetFloor);
}

public sealed record TowerEquipmentSupplyChoice(string Id, string Name, string Description,
    EquipmentType EquipmentType);

public sealed class TowerEquipmentSupplyCatalog
{
    private readonly EquipmentCatalog _equipment;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<TowerEquipmentSupplyChoice>> _choices;

    public TowerEquipmentSupplyCatalog(EquipmentCatalog equipment, IEnumerable<TowerEquipmentSupply> supplies)
    {
        _equipment = equipment;
        Supplies = Array.AsReadOnly(supplies.OrderBy(x => x.TargetFloor).ToArray());
        if (Supplies.Count == 0 || Supplies.Any(x => string.IsNullOrWhiteSpace(x.ItemBaseId)
            || string.IsNullOrWhiteSpace(x.Name) || x.TargetFloor < 1 || x.SourceRegion < 1
            || x.Tier < 1 || x.Tier > EquipmentTierBudgetCurve.MaximumSupportedTier
            || x.MinimumLevel < EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(x.Tier)
            || (x.TargetFloor - 1) % 10 is not (0 or 3 or 6 or 9))
            || Supplies.Select(x => x.ItemBaseId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Supplies.Count
            || Supplies.Select(x => (x.SourceRegion, x.TargetFloor)).Distinct().Count() != Supplies.Count)
            throw new ArgumentException("Invalid Tower equipment supply catalog.");

        _choices = Supplies.ToDictionary(supply => supply.ItemBaseId, supply =>
        {
            IReadOnlyList<TowerEquipmentSupplyChoice> choices = equipment.Evaluator.Definitions
                .Where(x => x.Rarity == supply.Band.Rarity && x.NativeStyleId is null)
                .Where(x => equipment.Evaluator.GetArchetype(x.ArchetypeId) is var archetype
                    && supply.Tier >= archetype.MinimumTier && supply.Tier <= archetype.MaximumTier)
                .Select(x => new TowerEquipmentSupplyChoice(x.Id,
                    $"{x.Name} — {Label(x.SpecializationId)}",
                    $"{Label(equipment.Evaluator.GetArchetype(x.ArchetypeId).EquipmentType.ToString())} · "
                    + $"Tier {supply.Tier} · {supply.Band.Rarity} · {supply.Band.Quality} · Rank {supply.Band.Rank}",
                    equipment.Evaluator.GetArchetype(x.ArchetypeId).EquipmentType))
                .OrderBy(x => x.EquipmentType).ThenBy(x => x.Name, StringComparer.Ordinal).ToArray();
            if (choices.Count == 0) throw new ArgumentException($"No equipment choices for '{supply.ItemBaseId}'.");
            return choices;
        }, StringComparer.Ordinal);
    }

    public IReadOnlyList<TowerEquipmentSupply> Supplies { get; }
    // Progress/release checks remain with the caller; both live issuance and offline
    // acquisition analysis use the same region, entry-level and priority policy.
    public IEnumerable<TowerEquipmentSupply> Candidates(int sourceRegion, int entryLevel) =>
        Supplies.Where(x => x.SourceRegion == sourceRegion && x.MinimumLevel <= entryLevel)
            .OrderByDescending(x => x.TargetFloor);
    public TowerEquipmentSupply? Find(string itemBaseId) => Supplies.FirstOrDefault(x => x.ItemBaseId == itemBaseId);
    public IReadOnlyList<TowerEquipmentSupplyChoice> Choices(string itemBaseId) =>
        _choices.GetValueOrDefault(itemBaseId) ?? [];

    public EquipmentData Award(string itemBaseId, string choiceId, Guid ownerId, Guid itemId, string openingId)
    {
        var supply = Find(itemBaseId) ?? throw new ArgumentException("Unknown Tower supply chest.");
        if (!Choices(itemBaseId).Any(x => x.Id == choiceId))
            throw new ArgumentException("Choose equipment from this chest's available options.");
        return EquipmentData.Create(EquipmentState.Award(itemId, _equipment.Evaluator, choiceId,
            supply.Tier, supply.Band.Rank,
            new(EquipmentAwardKind.ProtectedReward, itemBaseId, openingId),
            new(EquipmentOwnershipKind.BoundPersonal, ownerId), supply.Band.Quality, 1d), _equipment.Evaluator);
    }

    private static string Label(string value) => value switch
    {
        "default" => "Balanced", "OneHanded" => "One-handed", "TwoHanded" => "Two-handed", "OffHand" => "Off-hand",
        _ => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Replace('_', ' '))
    };
}
