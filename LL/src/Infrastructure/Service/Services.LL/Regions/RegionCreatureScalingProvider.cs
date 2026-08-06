using System.Text.Json;
using Application.Interfaces.Services.LL.Regions;
using Domain.Helpers.Constants;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Configuration;

namespace Services.LL.Regions;

public sealed class RegionCreatureScalingProvider : IRegionCreatureScalingProvider
{
    public const string DefaultProfileId = "legacy-area-v1";
    private readonly RegionCombatBalanceCatalog _catalog;
    private readonly IReadOnlyDictionary<string, RegionCombatBalanceProfile> _profiles;
    private readonly IReadOnlyDictionary<string, AreaPlacement> _areas;

    public RegionCreatureScalingProvider(
        IConfiguration configuration,
        string contentRootPath,
        JsonSerializerOptions options)
        : this(ReadCatalog(configuration, contentRootPath, options))
    {
    }

    internal RegionCreatureScalingProvider(RegionCombatBalanceCatalog catalog)
    {
        Validate(catalog);
        _catalog = catalog;
        _profiles = catalog.Profiles.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        _areas = catalog.Regions
            .SelectMany(region => region.AreaIds.Select((areaId, index) => new AreaPlacement(
                areaId,
                region.RegionKey,
                region.ProfileId,
                checked(region.StartingGlobalStep + index))))
            .ToDictionary(x => x.AreaId, StringComparer.OrdinalIgnoreCase);
    }

    public RegionCombatBalanceCatalog GetCatalog() => _catalog;

    public CreatureScalingProfile GetScaling(Area area)
    {
        ArgumentNullException.ThrowIfNull(area);

        if (_areas.TryGetValue(area.Id, out var placement))
        {
            return CreateScaling(
                _profiles[placement.ProfileId],
                placement.RegionKey,
                placement.GlobalStep);
        }

        var fallback = _profiles[DefaultProfileId];
        return CreateScaling(fallback, null, Math.Max(1, area.DifficultyTier));
    }

    public static IRegionCreatureScalingProvider CreateLegacyFallback() =>
        new RegionCreatureScalingProvider(new RegionCombatBalanceCatalog(
            1,
            [CreateLegacyProfile()],
            []));

    private static CreatureScalingProfile CreateScaling(
        RegionCombatBalanceProfile profile,
        string? regionKey,
        int globalStep)
    {
        var progressionStep = Math.Max(0, globalStep - 1);
        return new CreatureScalingProfile(
            profile.Id,
            regionKey,
            globalStep,
            progressionStep,
            Evaluate(profile.HealthCurve, progressionStep),
            Evaluate(profile.OffenseCurve, progressionStep),
            Evaluate(profile.DefenseCurve, progressionStep),
            Evaluate(profile.ResistanceCurve, progressionStep),
            1d + profile.AttackSpeedGrowthPerStep * progressionStep,
            1d + profile.PenetrationGrowthPerStep * progressionStep,
            1d + profile.SoftDefenseGrowthPerStep * progressionStep,
            profile.CritChancePerStep * progressionStep,
            profile.CritDamagePerStep * progressionStep,
            profile.CritChanceCap,
            profile.CritDamageCap);
    }

    private static double Evaluate(RegionCombatGrowthCurve curve, int progressionStep) =>
        curve.Model.Equals("Exponential", StringComparison.OrdinalIgnoreCase)
            ? curve.BaseMultiplier * Math.Pow(1d + curve.GrowthPerStep, progressionStep)
            : curve.BaseMultiplier * Math.Pow(1d + curve.GrowthPerStep * progressionStep, curve.Exponent);

    private static RegionCombatBalanceCatalog ReadCatalog(
        IConfiguration configuration,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = configuration["Content:Root"] ?? "Data";
        var path = Path.Combine(
            contentRootPath,
            contentRoot,
            "progression",
            "region-combat-balance.json");
        var document = JsonSerializer.Deserialize<RegionCombatBalanceDocument>(
                           File.ReadAllText(path),
                           options)
                       ?? throw new InvalidOperationException(
                           "Could not deserialize region combat balance content.");

        return new RegionCombatBalanceCatalog(
            document.Version,
            document.Profiles.Select(MapProfile).ToArray(),
            document.Regions.Select(region => new RegionCombatBalanceRegion(
                region.RegionKey,
                region.ProfileId,
                region.StartingGlobalStep,
                region.AreaIds)).ToArray());
    }

    private static RegionCombatBalanceProfile MapProfile(ProfileDocument profile) => new(
        profile.Id,
        profile.TargetWinRateBasisPoints,
        MapCurve(profile.HealthCurve),
        MapCurve(profile.OffenseCurve),
        MapCurve(profile.DefenseCurve),
        MapCurve(profile.ResistanceCurve),
        profile.AttackSpeedGrowthPerStep,
        profile.PenetrationGrowthPerStep,
        profile.SoftDefenseGrowthPerStep,
        profile.CritChancePerStep,
        profile.CritDamagePerStep,
        profile.CritChanceCap,
        profile.CritDamageCap,
        profile.MaximumStepIncrease);

    private static RegionCombatGrowthCurve MapCurve(GrowthCurveDocument curve) => new(
        curve.Model,
        curve.BaseMultiplier,
        curve.GrowthPerStep,
        curve.Exponent);

    private static void Validate(RegionCombatBalanceCatalog catalog)
    {
        if (catalog.Version <= 0 || catalog.Profiles.Count == 0)
            throw new InvalidOperationException("Region combat balance requires a positive version and at least one profile.");

        var duplicateProfiles = catalog.Profiles
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();
        if (duplicateProfiles.Length > 0)
            throw new InvalidOperationException($"Duplicate region combat profiles: {string.Join(", ", duplicateProfiles)}.");

        var profileIds = catalog.Profiles.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!profileIds.Contains(DefaultProfileId))
            throw new InvalidOperationException($"Region combat balance must define fallback profile '{DefaultProfileId}'.");

        foreach (var profile in catalog.Profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Id) ||
                profile.TargetWinRateBasisPoints is <= 0 or > 10_000 ||
                profile.MaximumStepIncrease < 0 ||
                profile.CritChanceCap is < 0 or > 1 ||
                profile.CritDamageCap < 1)
            {
                throw new InvalidOperationException($"Region combat profile '{profile.Id}' is invalid.");
            }

            ValidateCurve(profile.Id, nameof(profile.HealthCurve), profile.HealthCurve);
            ValidateCurve(profile.Id, nameof(profile.OffenseCurve), profile.OffenseCurve);
            ValidateCurve(profile.Id, nameof(profile.DefenseCurve), profile.DefenseCurve);
            ValidateCurve(profile.Id, nameof(profile.ResistanceCurve), profile.ResistanceCurve);
        }

        var areaIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var region in catalog.Regions)
        {
            if (string.IsNullOrWhiteSpace(region.RegionKey) ||
                !profileIds.Contains(region.ProfileId) ||
                region.StartingGlobalStep <= 0 ||
                region.AreaIds.Count == 0)
            {
                throw new InvalidOperationException($"Region combat entry '{region.RegionKey}' is invalid.");
            }

            foreach (var areaId in region.AreaIds)
            {
                if (string.IsNullOrWhiteSpace(areaId) || !areaIds.Add(areaId))
                    throw new InvalidOperationException($"Area '{areaId}' has invalid or duplicate region combat placement.");
            }
        }

        foreach (var region in catalog.Regions)
        {
            var profile = catalog.Profiles.Single(x =>
                x.Id.Equals(region.ProfileId, StringComparison.OrdinalIgnoreCase));
            CreatureScalingProfile? previous = null;
            for (var index = 0; index < region.AreaIds.Count; index++)
            {
                var current = CreateScaling(profile, region.RegionKey, region.StartingGlobalStep + index);
                if (previous is not null)
                {
                    ValidateStepIncrease(region.RegionKey, "health", previous.HealthMultiplier, current.HealthMultiplier, profile.MaximumStepIncrease);
                    ValidateStepIncrease(region.RegionKey, "offense", previous.OffenseMultiplier, current.OffenseMultiplier, profile.MaximumStepIncrease);
                    ValidateStepIncrease(region.RegionKey, "defense", previous.DefenseMultiplier, current.DefenseMultiplier, profile.MaximumStepIncrease);
                    ValidateStepIncrease(region.RegionKey, "resistance", previous.ResistanceMultiplier, current.ResistanceMultiplier, profile.MaximumStepIncrease);
                }
                previous = current;
            }
        }
    }

    private static void ValidateCurve(
        string profileId,
        string name,
        RegionCombatGrowthCurve curve)
    {
        if ((!curve.Model.Equals("Power", StringComparison.OrdinalIgnoreCase) &&
             !curve.Model.Equals("Exponential", StringComparison.OrdinalIgnoreCase)) ||
            curve.BaseMultiplier <= 0 || curve.GrowthPerStep < 0 || curve.Exponent <= 0)
            throw new InvalidOperationException($"{profileId}.{name} is invalid.");
    }

    private static void ValidateStepIncrease(
        string regionKey,
        string metric,
        double previous,
        double current,
        double maximumStepIncrease)
    {
        if (current < previous)
            throw new InvalidOperationException($"Region '{regionKey}' has decreasing {metric} scaling.");
        if (previous > 0 && current / previous - 1d > maximumStepIncrease)
        {
            throw new InvalidOperationException(
                $"Region '{regionKey}' {metric} scaling exceeds the allowed step increase.");
        }
    }

    private static RegionCombatBalanceProfile CreateLegacyProfile() => new(
        DefaultProfileId,
        8_500,
        new RegionCombatGrowthCurve("Power", 1, MonsterScalingConstants.HpA, MonsterScalingConstants.HpB),
        new RegionCombatGrowthCurve("Power", 1, MonsterScalingConstants.OffenseC, MonsterScalingConstants.OffenseExp),
        new RegionCombatGrowthCurve("Power", 1, MonsterScalingConstants.DefenseA, MonsterScalingConstants.DefenseB),
        new RegionCombatGrowthCurve("Power", 1, MonsterScalingConstants.ResistA, MonsterScalingConstants.ResistB),
        MonsterScalingConstants.AccuracyPerTier,
        MonsterScalingConstants.PenPerTier,
        0.05,
        MonsterScalingConstants.CritChancePerTier,
        MonsterScalingConstants.CritDamagePerTier,
        MonsterScalingConstants.CritChanceCap,
        MonsterScalingConstants.CritDamageCap,
        0.50);

    private sealed record AreaPlacement(
        string AreaId,
        string RegionKey,
        string ProfileId,
        int GlobalStep);

    private sealed class RegionCombatBalanceDocument
    {
        public int Version { get; set; }
        public List<ProfileDocument> Profiles { get; set; } = [];
        public List<RegionDocument> Regions { get; set; } = [];
    }

    private sealed class ProfileDocument
    {
        public string Id { get; set; } = string.Empty;
        public int TargetWinRateBasisPoints { get; set; }
        public GrowthCurveDocument HealthCurve { get; set; } = new();
        public GrowthCurveDocument OffenseCurve { get; set; } = new();
        public GrowthCurveDocument DefenseCurve { get; set; } = new();
        public GrowthCurveDocument ResistanceCurve { get; set; } = new();
        public double AttackSpeedGrowthPerStep { get; set; }
        public double PenetrationGrowthPerStep { get; set; }
        public double SoftDefenseGrowthPerStep { get; set; }
        public double CritChancePerStep { get; set; }
        public double CritDamagePerStep { get; set; }
        public float CritChanceCap { get; set; }
        public float CritDamageCap { get; set; }
        public double MaximumStepIncrease { get; set; }
    }

    private sealed class GrowthCurveDocument
    {
        public string Model { get; set; } = "Power";
        public double BaseMultiplier { get; set; }
        public double GrowthPerStep { get; set; }
        public double Exponent { get; set; }
    }

    private sealed class RegionDocument
    {
        public string RegionKey { get; set; } = string.Empty;
        public string ProfileId { get; set; } = string.Empty;
        public int StartingGlobalStep { get; set; }
        public List<string> AreaIds { get; set; } = [];
    }
}
