using System.Text.Json;
using Domain.Components.Attributes;
using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Configuration;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;
using Services.LL.Regions;

namespace Services.LL.Combat.Engine;

/// <summary>
/// Produces deterministic, Essence-free player attribute snapshots from
/// progression and equipment-budget assumptions authored as content data.
/// </summary>
public sealed class PlayerProgressionSnapshotFactory
{
    public const string DefaultManifestFileName = "player-progression-snapshots.json";
    private static readonly string[] RequiredEnvelopeIds = ["minimum", "expected", "optimized"];

    private readonly PlayerProgressionSnapshotDocument _document;
    private readonly CraftingBalanceOptions _craftingBalance;

    public PlayerProgressionSnapshotFactory(
        IConfiguration configuration,
        string contentRootPath,
        JsonSerializerOptions jsonOptions,
        CraftingBalanceOptions? craftingBalance = null)
        : this(ReadDocument(configuration, contentRootPath, jsonOptions), craftingBalance)
    {
    }

    internal PlayerProgressionSnapshotFactory(
        PlayerProgressionSnapshotDocument document,
        CraftingBalanceOptions? craftingBalance = null)
    {
        _document = document;
        _craftingBalance = craftingBalance ?? new CraftingBalanceOptions();
        ValidateDocument(document);
    }

    public PlayerProgressionSnapshotReport Generate()
    {
        var snapshots = new List<PlayerProgressionSnapshot>();
        foreach (var anchor in _document.Anchors.OrderBy(anchor => anchor.ProgressionPosition))
        {
            var regionNumber = ((anchor.ProgressionPosition - 1) / CanonicalRegionProgressionPolicy.AreasPerRegion) + 1;
            var regionStep = (anchor.ProgressionPosition - 1) % CanonicalRegionProgressionPolicy.AreasPerRegion;
            var regionProgress = regionStep / (double)(CanonicalRegionProgressionPolicy.AreasPerRegion - 1);
            var equipmentTier = CanonicalRegionProgressionPolicy.GetEquipmentTier(regionNumber);

            foreach (var envelope in _document.GearEnvelopes)
            {
                var currentTierShare = regionNumber == 1
                    ? 1d
                    : Interpolate(
                        envelope.CurrentTierShareAtRegionEntry,
                        envelope.CurrentTierShareAtRegionCompletion,
                        regionProgress);
                var previousTier = Math.Max(EquipmentStatBudgetCatalog.MinimumTier, equipmentTier - 1);
                var blendedTierBudget =
                    EquipmentTierBudgetCurve.GetBudget(previousTier) * (1d - currentTierShare)
                    + EquipmentTierBudgetCurve.GetBudget(equipmentTier) * currentTierShare;
                var totalEquipmentBudget = blendedTierBudget
                                           * _document.CombatLoadoutBudgetWeight
                                           * envelope.SlotFillPercent
                                           * _craftingBalance.GetQualityStatMultiplier(envelope.Quality)
                                           * envelope.RollMultiplier
                                           * (1d + envelope.TemperingBudgetPercent);

                foreach (var allocation in _document.AllocationProfiles)
                {
                    var equipmentPoints = MaterializeEquipmentPoints(
                        allocation.Weights,
                        totalEquipmentBudget,
                        equipmentTier);
                    var baseAttributes = AttributeCatalog.All.ToDictionary(
                        definition => definition.AttributeType,
                        definition => EntityBaseAttributeHelper.GetValueForCharacterLevel(
                            definition.AttributeType,
                            anchor.CharacterLevel));
                    var effectiveModifiers = equipmentPoints.Select(entry =>
                        new InstanceAttributeModifier(
                            entry.Key,
                            ResolveEffectiveEquipmentValue(entry.Key, entry.Value, equipmentTier),
                            ModifierType.Flat));
                    var attributes = AttributeCalculator.CalculateProjectedAttributes(
                        baseAttributes,
                        effectiveModifiers);
                    var combatRating = CombatRatingCalculator.CalculateCanonical(
                        baseAttributes,
                        equipmentPoints,
                        equipmentTier);

                    snapshots.Add(new PlayerProgressionSnapshot(
                        anchor.Id,
                        anchor.ProgressionPosition,
                        regionNumber,
                        regionStep + 1,
                        anchor.CharacterLevel,
                        equipmentTier,
                        envelope.Id,
                        allocation.Id,
                        currentTierShare,
                        totalEquipmentBudget,
                        equipmentPoints,
                        attributes,
                        combatRating,
                        CalculateUnmitigatedBasicPressure(attributes),
                        CalculateEffectiveDurability(attributes, AttributeType.Armor),
                        CalculateEffectiveDurability(attributes, AttributeType.Resistance)));
                }
            }
        }

        ValidateGeneratedOrdering(snapshots);
        return new PlayerProgressionSnapshotReport(_document.Version, snapshots);
    }

    private static Dictionary<AttributeType, double> MaterializeEquipmentPoints(
        IReadOnlyDictionary<AttributeType, double> weights,
        double totalBudget,
        int equipmentTier) =>
        weights.ToDictionary(
            entry => entry.Key,
            entry => totalBudget * entry.Value
                     / EquipmentStatBudgetCatalog.GetMaterializedCostPerPoint(entry.Key, equipmentTier));

    private static float ResolveEffectiveEquipmentValue(
        AttributeType attribute,
        double equipmentPoints,
        int equipmentTier) =>
        EquipmentStatBudgetCatalog.IsRating(attribute)
            ? EquipmentStatBudgetCatalog.ConvertRatingToEffectiveValue(
                attribute,
                equipmentPoints,
                equipmentTier)
            : (float)equipmentPoints;

    private static double CalculateUnmitigatedBasicPressure(
        IReadOnlyDictionary<AttributeType, float> attributes)
    {
        var power = attributes.GetValueOrDefault(AttributeType.Power);
        var attackRate = 1d + attributes.GetValueOrDefault(AttributeType.AttackSpeed) / 100d;
        var critChance = attributes.GetValueOrDefault(AttributeType.CritChance) / 100d;
        var critBonus = attributes.GetValueOrDefault(AttributeType.CritDamage) / 100d;
        return (1d + 0.5d * power) * attackRate * (1d + critChance * critBonus);
    }

    private static double CalculateEffectiveDurability(
        IReadOnlyDictionary<AttributeType, float> attributes,
        AttributeType typedDefense)
    {
        var health = attributes.GetValueOrDefault(AttributeType.MaxHealth);
        var typedMitigation = Math.Clamp(attributes.GetValueOrDefault(typedDefense) / 100d, 0d, 0.999d);
        var generalReduction = Math.Clamp(
            attributes.GetValueOrDefault(AttributeType.DamageReduction) / 100d,
            0d,
            0.999d);
        return health / ((1d - typedMitigation) * (1d - generalReduction));
    }

    private static void ValidateDocument(PlayerProgressionSnapshotDocument document)
    {
        if (document.Version <= 0 || document.CombatLoadoutBudgetWeight <= 0)
            throw new InvalidOperationException("Player progression snapshots require a positive version and combat loadout budget weight.");
        if (document.Anchors.Count == 0 || document.GearEnvelopes.Count == 0 || document.AllocationProfiles.Count == 0)
            throw new InvalidOperationException("Player progression snapshots require anchors, gear envelopes, and allocation profiles.");

        ThrowForDuplicateIds(document.Anchors.Select(anchor => anchor.Id), "anchor");
        ThrowForDuplicateIds(document.GearEnvelopes.Select(envelope => envelope.Id), "gear envelope");
        ThrowForDuplicateIds(document.AllocationProfiles.Select(profile => profile.Id), "allocation profile");

        var envelopeIds = document.GearEnvelopes.Select(envelope => envelope.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (RequiredEnvelopeIds.Any(required => !envelopeIds.Contains(required)))
        {
            throw new InvalidOperationException(
                $"Player progression snapshots require envelopes: {string.Join(", ", RequiredEnvelopeIds)}.");
        }

        foreach (var anchor in document.Anchors)
        {
            if (string.IsNullOrWhiteSpace(anchor.Id) || anchor.ProgressionPosition <= 0 || anchor.CharacterLevel <= 0)
                throw new InvalidOperationException("Every player progression anchor requires an id, positive position, and positive level.");

            var expectedLevel = anchor.ProgressionPosition == 1
                ? 1
                : checked(anchor.ProgressionPosition * CanonicalRegionProgressionPolicy.LevelsPerArea
                          - CanonicalRegionProgressionPolicy.LevelsPerArea);
            if (anchor.CharacterLevel != expectedLevel)
            {
                throw new InvalidOperationException(
                    $"{anchor.Id}: level {anchor.CharacterLevel} does not match canonical position {anchor.ProgressionPosition} level {expectedLevel}.");
            }

            var equipmentTier = ((anchor.ProgressionPosition - 1) / CanonicalRegionProgressionPolicy.AreasPerRegion) + 1;
            if (anchor.CharacterLevel < EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(equipmentTier))
                throw new InvalidOperationException($"{anchor.Id}: equipment tier {equipmentTier} is not legal at level {anchor.CharacterLevel}.");
        }

        foreach (var envelope in document.GearEnvelopes)
        {
            if (string.IsNullOrWhiteSpace(envelope.Id)
                || envelope.SlotFillPercent is <= 0 or > 1
                || envelope.RollMultiplier is < 0.5 or > 1.5
                || envelope.TemperingBudgetPercent is < 0 or > 1
                || envelope.CurrentTierShareAtRegionEntry is < 0 or > 1
                || envelope.CurrentTierShareAtRegionCompletion is < 0 or > 1
                || envelope.CurrentTierShareAtRegionCompletion < envelope.CurrentTierShareAtRegionEntry
                || !Enum.IsDefined(envelope.Quality))
            {
                throw new InvalidOperationException($"Gear envelope '{envelope.Id}' contains invalid assumptions.");
            }
        }

        foreach (var profile in document.AllocationProfiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Id) || profile.Weights.Count == 0)
                throw new InvalidOperationException("Every allocation profile requires an id and weights.");
            if (profile.Weights.Any(entry =>
                    entry.Value <= 0
                    || !EquipmentStatBudgetCatalog.IsKnown(entry.Key)
                    || !AttributeCatalog.IsEquipmentEligible(entry.Key)))
            {
                throw new InvalidOperationException($"Allocation profile '{profile.Id}' contains an invalid equipment attribute or weight.");
            }

            var totalWeight = profile.Weights.Values.Sum();
            if (Math.Abs(totalWeight - 1d) > 0.000_001d)
                throw new InvalidOperationException($"Allocation profile '{profile.Id}' weights sum to {totalWeight:0.######}, not 1.");
        }
    }

    private static void ValidateGeneratedOrdering(IReadOnlyList<PlayerProgressionSnapshot> snapshots)
    {
        foreach (var group in snapshots.GroupBy(snapshot => new { snapshot.AnchorId, snapshot.AllocationProfileId }))
        {
            var byEnvelope = group.ToDictionary(snapshot => snapshot.GearEnvelopeId, StringComparer.OrdinalIgnoreCase);
            if (byEnvelope["minimum"].TotalEquipmentBudget >= byEnvelope["expected"].TotalEquipmentBudget
                || byEnvelope["expected"].TotalEquipmentBudget >= byEnvelope["optimized"].TotalEquipmentBudget)
            {
                throw new InvalidOperationException(
                    $"{group.Key.AnchorId}/{group.Key.AllocationProfileId}: equipment budgets must increase from minimum to expected to optimized.");
            }
        }
    }

    private static void ThrowForDuplicateIds(IEnumerable<string> ids, string label)
    {
        var duplicate = ids.GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Invalid or duplicate {label} id '{duplicate.Key}'.");
    }

    private static double Interpolate(double start, double end, double progress) =>
        start + (end - start) * Math.Clamp(progress, 0d, 1d);

    private static PlayerProgressionSnapshotDocument ReadDocument(
        IConfiguration configuration,
        string contentRootPath,
        JsonSerializerOptions jsonOptions)
    {
        var contentRoot = configuration["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "progression", DefaultManifestFileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Could not find player progression snapshot manifest '{path}'.", path);

        return JsonSerializer.Deserialize<PlayerProgressionSnapshotDocument>(
                   File.ReadAllText(path),
                   jsonOptions)
               ?? throw new InvalidOperationException("Could not deserialize player progression snapshot manifest.");
    }
}

internal sealed class PlayerProgressionSnapshotDocument
{
    public int Version { get; set; }
    public double CombatLoadoutBudgetWeight { get; set; }
    public List<PlayerProgressionAnchorDefinition> Anchors { get; set; } = [];
    public List<PlayerGearEnvelopeDefinition> GearEnvelopes { get; set; } = [];
    public List<PlayerAttributeAllocationDefinition> AllocationProfiles { get; set; } = [];
}

internal sealed class PlayerProgressionAnchorDefinition
{
    public string Id { get; set; } = string.Empty;
    public int ProgressionPosition { get; set; }
    public int CharacterLevel { get; set; }
}

internal sealed class PlayerGearEnvelopeDefinition
{
    public string Id { get; set; } = string.Empty;
    public double SlotFillPercent { get; set; }
    public ItemQuality Quality { get; set; }
    public double RollMultiplier { get; set; }
    public double TemperingBudgetPercent { get; set; }
    public double CurrentTierShareAtRegionEntry { get; set; }
    public double CurrentTierShareAtRegionCompletion { get; set; }
}

internal sealed class PlayerAttributeAllocationDefinition
{
    public string Id { get; set; } = string.Empty;
    public Dictionary<AttributeType, double> Weights { get; set; } = [];
}

public sealed record PlayerProgressionSnapshotReport(
    int Version,
    IReadOnlyList<PlayerProgressionSnapshot> Snapshots);

public sealed record PlayerProgressionSnapshot(
    string AnchorId,
    int ProgressionPosition,
    int RegionNumber,
    int AreaNumber,
    int CharacterLevel,
    int EquipmentTier,
    string GearEnvelopeId,
    string AllocationProfileId,
    double CurrentTierShare,
    double TotalEquipmentBudget,
    IReadOnlyDictionary<AttributeType, double> EquipmentPoints,
    IReadOnlyDictionary<AttributeType, float> Attributes,
    CombatRatingBreakdown CombatRating,
    double UnmitigatedBasicPressure,
    double PhysicalEffectiveDurability,
    double MagicalEffectiveDurability);
