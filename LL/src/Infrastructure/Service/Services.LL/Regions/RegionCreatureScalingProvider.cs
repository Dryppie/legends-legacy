using System.Text.Json;
using Application.Interfaces.Services.LL.Regions;
using Domain.Helpers.Constants;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Configuration;

namespace Services.LL.Regions;

public sealed class RegionCreatureScalingProvider : IRegionCreatureScalingProvider
{
    public const string DefaultProfileId = "unified-global-v1";
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

    public RegionCreatureScalingProvider(RegionCombatBalanceCatalog catalog)
    {
        _catalog = catalog;
        Validate(catalog);
        _profiles = catalog.Profiles.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        _areas = catalog.Regions
            .SelectMany(region => region.AreaIds.Select((areaId, index) => new AreaPlacement(
                areaId,
                region.RegionKey,
                region.ProfileId,
                checked(region.StartingGlobalStep + index),
                index,
                InterpolateCombatRating(
                    region.StartingCombatRating,
                    region.EndingCombatRating,
                    index,
                    region.AreaIds.Count))))
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
                placement.GlobalStep,
                placement.RegionStep,
                placement.GlobalStep - 1,
                placement.RecommendedCombatRating);
        }

        var fallback = _profiles[_catalog.FallbackProfileId];
        var fallbackGlobalStep = Math.Max(1, area.DifficultyTier);
        return CreateScaling(
            fallback,
            null,
            fallbackGlobalStep,
            null,
            fallbackGlobalStep - 1,
            null);
    }

    public static IRegionCreatureScalingProvider CreateLegacyFallback() =>
        new RegionCreatureScalingProvider(new RegionCombatBalanceCatalog(
            1,
            new CombatProgressionFoundation(10, 1, 1),
            [CreateLegacyProfile()],
            []));

    private CreatureScalingProfile CreateScaling(
        RegionCombatBalanceProfile profile,
        string? regionKey,
        int globalStep,
        int? regionStep,
        int progressionStep,
        int? recommendedCombatRating)
    {
        progressionStep = Math.Max(0, progressionStep);
        return new CreatureScalingProfile(
            profile.Id,
            regionKey,
            globalStep,
            regionStep,
            progressionStep,
            recommendedCombatRating,
            Evaluate(profile.HealthCurve, progressionStep, globalStep),
            Evaluate(profile.OffenseCurve, progressionStep, globalStep),
            Evaluate(profile.DefenseCurve, progressionStep, globalStep),
            Evaluate(profile.ResistanceCurve, progressionStep, globalStep),
            profile.AttackSpeedGrowthPerStep * progressionStep,
            profile.PenetrationGrowthPerStep * progressionStep,
            profile.SoftDefenseGrowthPerStep * progressionStep,
            profile.CritChancePerStep * progressionStep,
            profile.CritDamagePerStep * progressionStep,
            profile.CritChanceCap,
            profile.CritDamageCap);
    }

    private double Evaluate(
        RegionCombatGrowthCurve curve,
        int progressionStep,
        int globalStep)
    {
        if (curve.Model.Equals("Foundation", StringComparison.OrdinalIgnoreCase))
        {
            return ApplyPostTutorialBonus(
                curve,
                progressionStep,
                curve.BaseMultiplier * Math.Pow(EvaluateFoundation(globalStep), curve.Exponent));
        }

        if (curve.Model.Equals("Polynomial", StringComparison.OrdinalIgnoreCase))
        {
            return ApplyPostTutorialBonus(
                curve,
                progressionStep,
                EvaluatePolynomial(curve, progressionStep));
        }

        var result = curve.Model.Equals("Exponential", StringComparison.OrdinalIgnoreCase)
            ? curve.BaseMultiplier * Math.Pow(1d + curve.GrowthPerStep, progressionStep)
            : curve.BaseMultiplier * Math.Pow(1d + curve.GrowthPerStep * progressionStep, curve.Exponent);
        return ApplyPostTutorialBonus(curve, progressionStep, result);
    }

    private static double EvaluatePolynomial(
        RegionCombatGrowthCurve curve,
        int progressionStep)
    {
        var polynomialStep = curve.LinearAfterStep.HasValue
            ? Math.Min(progressionStep, curve.LinearAfterStep.Value)
            : progressionStep;
        var growth = curve.GrowthPerStep * Math.Pow(polynomialStep, curve.Exponent);
        if (curve.LinearAfterStep.HasValue && progressionStep > polynomialStep)
        {
            var linearGrowthPerStep = curve.LinearGrowthPerStep
                                      ?? curve.GrowthPerStep
                                      * curve.Exponent
                                      * Math.Pow(polynomialStep, curve.Exponent - 1d);
            growth += (progressionStep - polynomialStep) * linearGrowthPerStep;
        }

        return curve.BaseMultiplier * (1d + growth);
    }

    private static double ApplyPostTutorialBonus(
        RegionCombatGrowthCurve curve,
        int progressionStep,
        double value)
    {
        if (progressionStep <= 0 || curve.PostTutorialBonus <= 0)
            return value;
        if (!curve.PostTutorialFloorEndStep.HasValue)
            return value + curve.PostTutorialBonus;

        if (progressionStep >= curve.PostTutorialFloorEndStep.Value)
            return value;

        var entryValue = EvaluatePolynomial(curve, 1) + curve.PostTutorialBonus;
        return Math.Max(value, entryValue);
    }

    private double EvaluateFoundation(int globalStep)
    {
        var zeroBasedPosition = Math.Max(0, globalStep - 1);
        var regionIndex = zeroBasedPosition / _catalog.Foundation.AreasPerRegion;
        var areaIndex = zeroBasedPosition % _catalog.Foundation.AreasPerRegion;
        var withinRegionSteps = checked(
            (_catalog.Foundation.AreasPerRegion - 1) * regionIndex + areaIndex);

        return Math.Pow(_catalog.Foundation.AreaGrowth, withinRegionSteps)
               * Math.Pow(_catalog.Foundation.RegionJump, regionIndex);
    }

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
            new CombatProgressionFoundation(
                document.Foundation.AreasPerRegion,
                document.Foundation.AreaGrowth,
                document.Foundation.RegionJump),
            document.Profiles.Select(MapProfile).ToArray(),
            document.Regions.Select(region => new RegionCombatBalanceRegion(
                region.RegionKey,
                region.ProfileId,
                region.StartingGlobalStep,
                region.StartingCombatRating,
                region.EndingCombatRating,
                region.AreaIds,
                region.DefaultBuildIds)).ToArray(),
            document.FallbackProfileId);
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
        profile.MaximumStepIncrease,
        profile.MaximumFirstStepIncrease);

    private static RegionCombatGrowthCurve MapCurve(GrowthCurveDocument curve) => new(
        curve.Model,
        curve.BaseMultiplier,
        curve.GrowthPerStep,
        curve.Exponent,
        curve.LinearAfterStep,
        curve.LinearGrowthPerStep,
        curve.PostTutorialBonus,
        curve.PostTutorialFloorEndStep);

    private void Validate(RegionCombatBalanceCatalog catalog)
    {
        if (catalog.Version <= 0 || catalog.Profiles.Count == 0)
            throw new InvalidOperationException("Region combat balance requires a positive version and at least one profile.");

        if (catalog.Foundation.AreasPerRegion <= 0
            || !double.IsFinite(catalog.Foundation.AreaGrowth)
            || catalog.Foundation.AreaGrowth < 1
            || !double.IsFinite(catalog.Foundation.RegionJump)
            || catalog.Foundation.RegionJump < 1)
        {
            throw new InvalidOperationException("Region combat balance has an invalid progression foundation.");
        }

        var duplicateProfiles = catalog.Profiles
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();
        if (duplicateProfiles.Length > 0)
            throw new InvalidOperationException($"Duplicate region combat profiles: {string.Join(", ", duplicateProfiles)}.");

        var profileIds = catalog.Profiles.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(catalog.FallbackProfileId)
            || !profileIds.Contains(catalog.FallbackProfileId))
        {
            throw new InvalidOperationException(
                $"Region combat balance must define fallback profile '{catalog.FallbackProfileId}'.");
        }

        foreach (var profile in catalog.Profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Id) ||
                profile.TargetWinRateBasisPoints is <= 0 or > 10_000 ||
                profile.MaximumStepIncrease < 0 ||
                profile.MaximumFirstStepIncrease is < 0 ||
                profile.CritChanceCap is < 0 or > 100 ||
                profile.CritDamageCap is < 0 or > 500)
            {
                throw new InvalidOperationException($"Region combat profile '{profile.Id}' is invalid.");
            }

            ValidateCurve(profile.Id, nameof(profile.HealthCurve), profile.HealthCurve);
            ValidateCurve(profile.Id, nameof(profile.OffenseCurve), profile.OffenseCurve);
            ValidateCurve(profile.Id, nameof(profile.DefenseCurve), profile.DefenseCurve);
            ValidateCurve(profile.Id, nameof(profile.ResistanceCurve), profile.ResistanceCurve);
        }

        var areaIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var regionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var region in catalog.Regions)
        {
            if (string.IsNullOrWhiteSpace(region.RegionKey) ||
                !regionKeys.Add(region.RegionKey) ||
                !profileIds.Contains(region.ProfileId) ||
                region.StartingGlobalStep <= 0 ||
                region.StartingCombatRating <= 0 ||
                region.EndingCombatRating < region.StartingCombatRating ||
                region.AreaIds.Count == 0 ||
                region.DefaultBuildIds.Count != region.AreaIds.Count ||
                region.DefaultBuildIds.Any(string.IsNullOrWhiteSpace))
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
                var current = CreateScaling(
                    profile,
                    region.RegionKey,
                    region.StartingGlobalStep + index,
                    index,
                    region.StartingGlobalStep + index - 1,
                    InterpolateCombatRating(
                        region.StartingCombatRating,
                        region.EndingCombatRating,
                        index,
                        region.AreaIds.Count));
                if (previous is not null)
                {
                    var maximumStepIncrease = index == 1
                        ? profile.MaximumFirstStepIncrease ?? profile.MaximumStepIncrease
                        : profile.MaximumStepIncrease;
                    ValidateStepIncrease(region.RegionKey, "health", previous.HealthMultiplier, current.HealthMultiplier, maximumStepIncrease);
                    ValidateStepIncrease(region.RegionKey, "offense", previous.OffenseMultiplier, current.OffenseMultiplier, maximumStepIncrease);
                    ValidateStepIncrease(region.RegionKey, "defense", previous.DefenseMultiplier, current.DefenseMultiplier, maximumStepIncrease);
                    ValidateStepIncrease(region.RegionKey, "resistance", previous.ResistanceMultiplier, current.ResistanceMultiplier, maximumStepIncrease);
                }
                previous = current;
            }
        }

        var orderedRegions = catalog.Regions
            .OrderBy(region => region.StartingGlobalStep)
            .ToArray();
        if (orderedRegions.Length > 0 && orderedRegions[0].StartingGlobalStep != 1)
        {
            throw new InvalidOperationException(
                "The first region combat entry must begin at global step 1.");
        }

        for (var index = 1; index < orderedRegions.Length; index++)
        {
            var previous = orderedRegions[index - 1];
            var current = orderedRegions[index];
            var expectedGlobalStep = checked(
                previous.StartingGlobalStep + previous.AreaIds.Count);
            if (current.StartingGlobalStep != expectedGlobalStep)
            {
                throw new InvalidOperationException(
                    $"Region '{current.RegionKey}' must begin at global step " +
                    $"{expectedGlobalStep}, after '{previous.RegionKey}'.");
            }

            if (current.StartingCombatRating < previous.EndingCombatRating)
            {
                throw new InvalidOperationException(
                    $"Region '{current.RegionKey}' starts below the ending Combat Rating " +
                    $"of '{previous.RegionKey}'.");
            }
        }
    }

    private static void ValidateCurve(
        string profileId,
        string name,
        RegionCombatGrowthCurve curve)
    {
        if ((!curve.Model.Equals("Power", StringComparison.OrdinalIgnoreCase) &&
             !curve.Model.Equals("Exponential", StringComparison.OrdinalIgnoreCase) &&
             !curve.Model.Equals("Foundation", StringComparison.OrdinalIgnoreCase) &&
             !curve.Model.Equals("Polynomial", StringComparison.OrdinalIgnoreCase)) ||
            curve.BaseMultiplier <= 0 || curve.GrowthPerStep < 0 || curve.Exponent <= 0 ||
            curve.LinearAfterStep is <= 0 || curve.LinearGrowthPerStep is < 0 ||
            curve.LinearGrowthPerStep.HasValue && !curve.LinearAfterStep.HasValue ||
            curve.PostTutorialBonus < 0 || curve.PostTutorialFloorEndStep is <= 1 ||
            curve.PostTutorialFloorEndStep.HasValue
            && (curve.PostTutorialBonus <= 0
                || !curve.Model.Equals("Polynomial", StringComparison.OrdinalIgnoreCase)))
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
        int GlobalStep,
        int RegionStep,
        int RecommendedCombatRating);

    private static int InterpolateCombatRating(
        int startingCombatRating,
        int endingCombatRating,
        int areaIndex,
        int areaCount)
    {
        if (areaCount <= 1 || startingCombatRating == endingCombatRating)
            return startingCombatRating;

        var progress = areaIndex / (double)(areaCount - 1);
        var multiplier = Math.Pow(
            endingCombatRating / (double)startingCombatRating,
            progress);
        return (int)Math.Round(startingCombatRating * multiplier);
    }

    private sealed class RegionCombatBalanceDocument
    {
        public int Version { get; set; }
        public string FallbackProfileId { get; set; } = DefaultProfileId;
        public FoundationDocument Foundation { get; set; } = new();
        public List<ProfileDocument> Profiles { get; set; } = [];
        public List<RegionDocument> Regions { get; set; } = [];
    }

    private sealed class FoundationDocument
    {
        public int AreasPerRegion { get; set; }
        public double AreaGrowth { get; set; }
        public double RegionJump { get; set; }
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
        public double? MaximumFirstStepIncrease { get; set; }
    }

    private sealed class GrowthCurveDocument
    {
        public string Model { get; set; } = "Power";
        public double BaseMultiplier { get; set; }
        public double GrowthPerStep { get; set; }
        public double Exponent { get; set; }
        public int? LinearAfterStep { get; set; }
        public double? LinearGrowthPerStep { get; set; }
        public double PostTutorialBonus { get; set; }
        public int? PostTutorialFloorEndStep { get; set; }
    }

    private sealed class RegionDocument
    {
        public string RegionKey { get; set; } = string.Empty;
        public string ProfileId { get; set; } = string.Empty;
        public int StartingGlobalStep { get; set; }
        public int StartingCombatRating { get; set; }
        public int EndingCombatRating { get; set; }
        public List<string> AreaIds { get; set; } = [];
        public List<string> DefaultBuildIds { get; set; } = [];
    }
}
