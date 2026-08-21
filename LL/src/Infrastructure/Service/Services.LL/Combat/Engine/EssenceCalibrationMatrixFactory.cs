using System.Text.Json;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Microsoft.Extensions.Configuration;

namespace Services.LL.Combat.Engine;

/// <summary>
/// Joins Essence-free player snapshots with independently authored Essence
/// loadout envelopes. Gear and Essence attainment remain separate dimensions.
/// </summary>
public sealed class EssenceCalibrationMatrixFactory
{
    public const string DefaultManifestFileName = "essence-calibration-loadouts.json";
    private static readonly string[] RequiredEnvelopeIds =
        ["attributes-only", "minimum", "expected", "optimized"];

    private readonly EssenceCalibrationMatrixDocument _document;
    private readonly PlayerProgressionSnapshotFactory _snapshotFactory;
    private readonly IEssenceSlotUnlockService _slotUnlocks;

    public EssenceCalibrationMatrixFactory(
        IConfiguration configuration,
        string contentRootPath,
        JsonSerializerOptions jsonOptions,
        PlayerProgressionSnapshotFactory snapshotFactory,
        IEssenceSlotUnlockService slotUnlocks)
        : this(
            ReadDocument(configuration, contentRootPath, jsonOptions),
            snapshotFactory,
            slotUnlocks)
    {
    }

    internal EssenceCalibrationMatrixFactory(
        EssenceCalibrationMatrixDocument document,
        PlayerProgressionSnapshotFactory snapshotFactory,
        IEssenceSlotUnlockService slotUnlocks)
    {
        _document = document;
        _snapshotFactory = snapshotFactory;
        _slotUnlocks = slotUnlocks;
        ValidateDocument(document);
    }

    public IReadOnlyList<EssenceProgressionCalibrationScenario> CreateScenarios()
    {
        var snapshots = _snapshotFactory.Generate().Snapshots;
        ValidateReferences(snapshots);
        var scenarios = new List<EssenceProgressionCalibrationScenario>();

        foreach (var anchorId in _document.AnchorIds)
        {
            foreach (var gearEnvelopeId in _document.GearEnvelopeIds)
            {
                foreach (var family in _document.BuildFamilies)
                {
                    var snapshot = snapshots.Single(candidate =>
                        candidate.AnchorId.Equals(anchorId, StringComparison.OrdinalIgnoreCase)
                        && candidate.GearEnvelopeId.Equals(gearEnvelopeId, StringComparison.OrdinalIgnoreCase)
                        && candidate.AllocationProfileId.Equals(
                            family.AllocationProfileId,
                            StringComparison.OrdinalIgnoreCase));
                    var unlockedSlots = _slotUnlocks.GetUnlockedSlotCount(snapshot.CharacterLevel);
                    var envelopes = _document.EssenceEnvelopes
                        .Select(envelope => CreateEnvelope(envelope, family, unlockedSlots))
                        .ToList();
                    var targetAttributes = new Dictionary<AttributeType, float>
                    {
                        [AttributeType.MaxHealth] = 100_000_000,
                        [AttributeType.Power] = Math.Max(
                            1,
                            snapshot.Attributes.GetValueOrDefault(AttributeType.Power)
                            * (float)_document.TargetPowerFraction),
                        [AttributeType.CritChance] = 0,
                        [AttributeType.CritDamage] = 100,
                        [AttributeType.DodgeChance] = 0
                    };

                    scenarios.Add(new EssenceProgressionCalibrationScenario(
                        $"{anchorId}.{gearEnvelopeId}.{family.Id}",
                        snapshot.ProgressionPosition,
                        snapshot.CharacterLevel,
                        snapshot.Attributes,
                        targetAttributes,
                        envelopes,
                        _document.RandomSeeds,
                        _document.MaxTicks,
                        _document.PlayerStartingHealthPercent,
                        TargetCanBasicAttack: true,
                        SnapshotAnchorId: anchorId,
                        GearEnvelopeId: gearEnvelopeId,
                        AllocationProfileId: family.AllocationProfileId,
                        BuildFamilyId: family.Id));
                }
            }
        }

        return scenarios;
    }

    private static EssenceProgressionCalibrationEnvelope CreateEnvelope(
        EssenceCalibrationEnvelopeDefinition envelope,
        EssenceCalibrationBuildFamilyDefinition family,
        int unlockedSlots)
    {
        var equippedCount = envelope.SlotFillPercent <= 0
            ? 0
            : Math.Max(1, (int)Math.Ceiling(unlockedSlots * envelope.SlotFillPercent));
        if (equippedCount > family.EssenceIds.Count)
        {
            throw new InvalidOperationException(
                $"Build family '{family.Id}' only defines {family.EssenceIds.Count} Essences but envelope '{envelope.Id}' needs {equippedCount}.");
        }

        return new EssenceProgressionCalibrationEnvelope(
            envelope.Id,
            family.EssenceIds.Take(equippedCount)
                .Select(id => new EssenceProgressionCalibrationEssence(
                    id,
                    envelope.AscensionTier,
                    envelope.IsEvolved))
                .ToList());
    }

    private void ValidateReferences(IReadOnlyList<PlayerProgressionSnapshot> snapshots)
    {
        var anchorIds = snapshots.Select(snapshot => snapshot.AnchorId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var gearEnvelopeIds = snapshots.Select(snapshot => snapshot.GearEnvelopeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allocationIds = snapshots.Select(snapshot => snapshot.AllocationProfileId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var anchorId in _document.AnchorIds.Where(id => !anchorIds.Contains(id)))
            throw new InvalidOperationException($"Essence calibration references unknown snapshot anchor '{anchorId}'.");
        foreach (var envelopeId in _document.GearEnvelopeIds.Where(id => !gearEnvelopeIds.Contains(id)))
            throw new InvalidOperationException($"Essence calibration references unknown gear envelope '{envelopeId}'.");
        foreach (var family in _document.BuildFamilies.Where(family =>
                     !allocationIds.Contains(family.AllocationProfileId)))
        {
            throw new InvalidOperationException(
                $"Essence build family '{family.Id}' references unknown allocation profile '{family.AllocationProfileId}'.");
        }
    }

    private static void ValidateDocument(EssenceCalibrationMatrixDocument document)
    {
        if (document.Version <= 0
            || document.AnchorIds.Count == 0
            || document.GearEnvelopeIds.Count == 0
            || document.RandomSeeds.Count == 0
            || document.MaxTicks <= 0
            || document.PlayerStartingHealthPercent is <= 0 or > 1
            || document.TargetPowerFraction is < 0 or > 1)
        {
            throw new InvalidOperationException("Essence calibration matrix contains invalid global assumptions.");
        }

        ThrowForDuplicateIds(document.AnchorIds, "anchor reference");
        ThrowForDuplicateIds(document.GearEnvelopeIds, "gear-envelope reference");
        ThrowForDuplicateIds(document.EssenceEnvelopes.Select(envelope => envelope.Id), "Essence envelope");
        ThrowForDuplicateIds(document.BuildFamilies.Select(family => family.Id), "build family");

        var envelopeIds = document.EssenceEnvelopes.Select(envelope => envelope.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (RequiredEnvelopeIds.Any(required => !envelopeIds.Contains(required))
            || document.EssenceEnvelopes.Count != RequiredEnvelopeIds.Length)
        {
            throw new InvalidOperationException(
                $"Essence calibration requires exactly these envelopes: {string.Join(", ", RequiredEnvelopeIds)}.");
        }

        foreach (var envelope in document.EssenceEnvelopes)
        {
            if (envelope.SlotFillPercent is < 0 or > 1
                || envelope.AscensionTier is < 0 or > 3
                || envelope.Id.Equals("attributes-only", StringComparison.OrdinalIgnoreCase)
                && (envelope.SlotFillPercent != 0 || envelope.AscensionTier != 0 || envelope.IsEvolved))
            {
                throw new InvalidOperationException($"Essence envelope '{envelope.Id}' contains invalid assumptions.");
            }
        }

        foreach (var family in document.BuildFamilies)
        {
            if (string.IsNullOrWhiteSpace(family.AllocationProfileId) || family.EssenceIds.Count == 0)
                throw new InvalidOperationException($"Essence build family '{family.Id}' is incomplete.");
            ThrowForDuplicateIds(family.EssenceIds, $"Essence in build family '{family.Id}'");
        }
    }

    private static void ThrowForDuplicateIds(IEnumerable<string> ids, string label)
    {
        var duplicate = ids.GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Invalid or duplicate {label} id '{duplicate.Key}'.");
    }

    private static EssenceCalibrationMatrixDocument ReadDocument(
        IConfiguration configuration,
        string contentRootPath,
        JsonSerializerOptions jsonOptions)
    {
        var contentRoot = configuration["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "progression", DefaultManifestFileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Could not find Essence calibration manifest '{path}'.", path);

        return JsonSerializer.Deserialize<EssenceCalibrationMatrixDocument>(
                   File.ReadAllText(path),
                   jsonOptions)
               ?? throw new InvalidOperationException("Could not deserialize Essence calibration manifest.");
    }
}

public sealed class EssenceCalibrationMatrixRunner
{
    private readonly EssenceCalibrationMatrixFactory _matrixFactory;
    private readonly EssenceProgressionCalibrationRunner _calibrationRunner;

    public EssenceCalibrationMatrixRunner(
        EssenceCalibrationMatrixFactory matrixFactory,
        EssenceProgressionCalibrationRunner calibrationRunner)
    {
        _matrixFactory = matrixFactory;
        _calibrationRunner = calibrationRunner;
    }

    public EssenceProgressionCalibrationReport Run() =>
        _calibrationRunner.Run(_matrixFactory.CreateScenarios());
}

internal sealed class EssenceCalibrationMatrixDocument
{
    public int Version { get; set; }
    public List<string> AnchorIds { get; set; } = [];
    public List<string> GearEnvelopeIds { get; set; } = [];
    public List<int> RandomSeeds { get; set; } = [];
    public int MaxTicks { get; set; }
    public float PlayerStartingHealthPercent { get; set; }
    public double TargetPowerFraction { get; set; }
    public List<EssenceCalibrationEnvelopeDefinition> EssenceEnvelopes { get; set; } = [];
    public List<EssenceCalibrationBuildFamilyDefinition> BuildFamilies { get; set; } = [];
}

internal sealed class EssenceCalibrationEnvelopeDefinition
{
    public string Id { get; set; } = string.Empty;
    public double SlotFillPercent { get; set; }
    public int AscensionTier { get; set; }
    public bool IsEvolved { get; set; }
}

internal sealed class EssenceCalibrationBuildFamilyDefinition
{
    public string Id { get; set; } = string.Empty;
    public string AllocationProfileId { get; set; } = string.Empty;
    public List<string> EssenceIds { get; set; } = [];
}
