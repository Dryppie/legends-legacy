using System.Globalization;
using System.Text.Json;
using Application.Interfaces.Services.LL.WorldTower;
using Common.Randomness;
using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates;
using Domain.Models.Entities.Creatures.Templates.Enums;
using Domain.Models.Regions.Areas;
using Domain.Models.Snapshots;
using Domain.Models.WorldTower;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.WorldTower;
using Services.LL.WorldTower;

namespace LegendsLegacy.Balance;

public enum WorldTowerDifficultyClassification
{
    TooHard,
    OnTarget,
    TooEasy
}

public sealed record WorldTowerAnalysisOptions(
    int SimulationsPerFloor = 10,
    double DesiredClearRate = 0.65,
    double ClearRateTolerance = 0.10,
    int MaxTicks = 6_000)
{
    public WorldTowerAnalysisOptions Validate()
    {
        if (SimulationsPerFloor is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(SimulationsPerFloor), "Simulations per floor must be between 1 and 1,000.");
        if (!double.IsFinite(DesiredClearRate) || DesiredClearRate is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(DesiredClearRate), "Desired clear rate must be between 0 and 1 exclusive.");
        if (!double.IsFinite(ClearRateTolerance) || ClearRateTolerance is < 0 or >= 0.5)
            throw new ArgumentOutOfRangeException(nameof(ClearRateTolerance), "Clear-rate tolerance must be between 0 and 0.5 exclusive.");
        if (MaxTicks is < 1 or > 100_000)
            throw new ArgumentOutOfRangeException(nameof(MaxTicks), "Maximum combat ticks must be between 1 and 100,000.");
        return this;
    }
}

public sealed record WorldTowerTrialSnapshot(
    int Trial,
    int CombatSeed,
    string Outcome,
    int DurationTicks,
    int FriendlyDeaths,
    double RemainingHealthRatio,
    double MeanPlayerDisplayCr,
    int TeamDisplayCr,
    IReadOnlyList<string> BuildIds);

public sealed record WorldTowerFloorAnalysisSnapshot(
    int Floor,
    string EncounterName,
    string GuardianName,
    string GuardianAbilityProfileId,
    int RequiredSlots,
    double TargetBenchmarkPower,
    string RepresentativeProfileId,
    double RepresentativeProfilePower,
    int RepresentativeBuildCount,
    int AuthoredRecommendedCr,
    double AuthoredHealthMultiplier,
    double AuthoredDamageMultiplier,
    double RecommendedDisplayCr,
    double? ObservedClearingDisplayCr,
    double DesiredClearRate,
    double ObservedClearRate,
    double AverageDurationTicks,
    double MedianDurationTicks,
    double AverageFriendlyDeaths,
    double AverageRemainingHealthRatio,
    WorldTowerDifficultyClassification Classification,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<WorldTowerTrialSnapshot> Trials);

public sealed record WorldTowerAnalysisSnapshot(
    int AlgorithmVersion,
    WorldTowerAnalysisOptions Options,
    IReadOnlyList<WorldTowerFloorAnalysisSnapshot> Floors);

/// <summary>
/// Runs the authored World Tower Region 1 encounters against deterministic P75
/// representatives. The adapter materializes the balance tool's detached
/// canonical builds, then uses production combat preparation, guardian scaling,
/// authored abilities, and the production combat engine.
/// </summary>
public sealed class WorldTowerContentAnalyzer(
    IWorldTowerDefinitionProvider towerDefinitions,
    IReadOnlyDictionary<Guid, Creature> creatures,
    ICombatSetupService combatSetup,
    ICombatEngineExecutor combatEngine,
    GearPackageFactory gearPackages) : IEncounterCalibrationEvaluator, IEncounterBuildEvaluator
{
    public const int AlgorithmVersion = 1;
    public const string RegionOneBandId = "WorldTower.Region1";

    public WorldTowerAnalysisSnapshot Analyze(
        ProgressionBandSuiteSnapshot progressionBands,
        PowerAnchorSuiteSnapshot powerAnchors,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        int runSeed,
        WorldTowerAnalysisOptions? requestedOptions = null)
    {
        ArgumentNullException.ThrowIfNull(progressionBands);
        ArgumentNullException.ThrowIfNull(powerAnchors);
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        var options = (requestedOptions ?? new WorldTowerAnalysisOptions()).Validate();
        var band = progressionBands.Bands.SingleOrDefault(candidate =>
                       candidate.Definition.Id.Equals(RegionOneBandId, StringComparison.Ordinal))
                   ?? throw new InvalidOperationException($"Progression band '{RegionOneBandId}' was not found.");
        var definitions = towerDefinitions.GetFloors()
            .Where(floor => floor.FloorNumber >= band.Definition.StartFloor
                            && floor.FloorNumber <= band.Definition.EndFloor)
            .OrderBy(floor => floor.FloorNumber)
            .ToArray();
        if (definitions.Length != band.Floors.Count
            || !definitions.Select(floor => floor.FloorNumber).SequenceEqual(band.Floors.Select(floor => floor.Floor)))
        {
            throw new InvalidOperationException(
                $"World Tower content does not define every floor in progression band '{RegionOneBandId}'.");
        }

        var p75Profiles = representativeBuilds.Profiles
            .Where(profile => profile.TargetPercentile == 75)
            .OrderBy(profile => profile.SlotCount)
            .ToArray();
        if (p75Profiles.Length == 0)
            throw new InvalidOperationException("World Tower analysis requires at least one P75 representative profile.");
        var startCr = ResolveAnchorCr(powerAnchors, band.Definition.StartAnchorId);
        var endCr = ResolveAnchorCr(powerAnchors, band.Definition.EndAnchorId);

        var floors = definitions.Select(definition => AnalyzeFloor(
                definition,
                band.Floors.Single(floor => floor.Floor == definition.FloorNumber),
                SelectProfile(p75Profiles, band.Floors.Single(floor => floor.Floor == definition.FloorNumber).TargetBenchmarkPower),
                startCr,
                endCr,
                runSeed,
                options))
            .ToArray();
        return new WorldTowerAnalysisSnapshot(AlgorithmVersion, options, floors);
    }

    private WorldTowerFloorAnalysisSnapshot AnalyzeFloor(
        TowerFloorDefinition definition,
        ProgressionFloorTargetSnapshot target,
        RepresentativeEssenceProfileSnapshot profile,
        double startCr,
        double endCr,
        int runSeed,
        WorldTowerAnalysisOptions options)
    {
        if (!creatures.TryGetValue(definition.GuardianCreatureId, out var guardianSource))
            throw new InvalidOperationException($"World Tower guardian '{definition.GuardianCreatureId}' was not found in creatures.json.");
        if (profile.Builds.Count == 0)
            throw new InvalidOperationException($"Representative profile '{profile.Id}' contains no builds.");

        var preparedBuilds = profile.Builds.Select(PrepareBuild).ToArray();
        var resolvedGuardianProfile = Domain.Models.Essences.CreatureEssenceSource.GetMonsterDefinitionId(guardianSource);
        if (!resolvedGuardianProfile.Equals(definition.GuardianAbilityProfileId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"World Tower floor {definition.FloorNumber} expects guardian ability profile " +
                $"'{definition.GuardianAbilityProfileId}', but creature '{guardianSource.Name}' resolves to " +
                $"'{resolvedGuardianProfile}'.");
        }

        var trials = Enumerable.Range(1, options.SimulationsPerFloor)
            .Select(trial => RunTrial(
                definition,
                preparedBuilds,
                guardianSource,
                runSeed,
                trial,
                options.MaxTicks))
            .ToArray();
        var victories = trials.Where(trial => trial.Outcome == BattleOutcome.Victory.ToString()).ToArray();
        var clearRate = victories.Length / (double)trials.Length;
        var recommendedCr = startCr + (endCr - startCr) * target.CurveWeight;
        var observedClearingCr = victories.Length == 0
            ? (double?)null
            : Median(victories.Select(trial => trial.MeanPlayerDisplayCr).OrderBy(value => value).ToArray());
        var classification = clearRate < options.DesiredClearRate - options.ClearRateTolerance
            ? WorldTowerDifficultyClassification.TooHard
            : clearRate > options.DesiredClearRate + options.ClearRateTolerance
                ? WorldTowerDifficultyClassification.TooEasy
                : WorldTowerDifficultyClassification.OnTarget;
        var warnings = CreateWarnings(
            definition,
            profile,
            trials,
            victories.Length,
            recommendedCr,
            classification);

        return new WorldTowerFloorAnalysisSnapshot(
            definition.FloorNumber,
            definition.Name,
            definition.GuardianName,
            definition.GuardianAbilityProfileId,
            definition.RequiredSlots,
            target.TargetBenchmarkPower,
            profile.Id,
            profile.MeanSelectedScore,
            profile.Builds.Count,
            definition.RecommendedPowerRating,
            Round(definition.GuardianScaling.Health, 3),
            Round(definition.GuardianScaling.Offense, 3),
            Round(recommendedCr, 2),
            observedClearingCr.HasValue ? Round(observedClearingCr.Value, 2) : null,
            Round(options.DesiredClearRate, 4),
            Round(clearRate, 4),
            Round(trials.Average(trial => trial.DurationTicks), 2),
            Round(Median(trials.Select(trial => (double)trial.DurationTicks).OrderBy(value => value).ToArray()), 2),
            Round(trials.Average(trial => trial.FriendlyDeaths), 2),
            Round(trials.Average(trial => trial.RemainingHealthRatio), 4),
            classification,
            warnings,
            trials);
    }

    public EncounterCalibrationEvaluation Evaluate(EncounterCalibrationEvaluationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.RepresentativeBuilds);
        if (!double.IsFinite(request.HealthAdjustmentFactor) || request.HealthAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.HealthAdjustmentFactor));
        if (!double.IsFinite(request.DamageAdjustmentFactor) || request.DamageAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.DamageAdjustmentFactor));
        if (request.Simulations is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(request.Simulations));
        if (request.MaxTicks is < 1 or > 100_000)
            throw new ArgumentOutOfRangeException(nameof(request.MaxTicks));

        var definition = towerDefinitions.GetFloors().SingleOrDefault(floor => floor.FloorNumber == request.Floor)
                         ?? throw new InvalidOperationException($"World Tower floor {request.Floor} was not found.");
        var profile = request.RepresentativeBuilds.Profiles.SingleOrDefault(candidate =>
                          candidate.Id.Equals(request.RepresentativeProfileId, StringComparison.Ordinal))
                      ?? throw new InvalidOperationException(
                          $"Representative profile '{request.RepresentativeProfileId}' was not found.");
        if (profile.Builds.Count == 0)
            throw new InvalidOperationException($"Representative profile '{profile.Id}' contains no builds.");
        if (!creatures.TryGetValue(definition.GuardianCreatureId, out var guardianSource))
            throw new InvalidOperationException($"World Tower guardian '{definition.GuardianCreatureId}' was not found in creatures.json.");
        var calibratedDefinition = WithCalibration(
            definition,
            request.HealthAdjustmentFactor,
            request.DamageAdjustmentFactor);
        var preparedBuilds = profile.Builds.Select(PrepareBuild).ToArray();
        var trials = Enumerable.Range(1, request.Simulations)
            .Select(trial => RunTrial(
                calibratedDefinition,
                preparedBuilds,
                guardianSource,
                request.RunSeed,
                trial,
                request.MaxTicks))
            .ToArray();
        return new EncounterCalibrationEvaluation(
            trials.Length,
            Round(trials.Count(trial => trial.Outcome == BattleOutcome.Victory.ToString()) / (double)trials.Length, 4),
            Round(trials.Average(trial => trial.DurationTicks), 2),
            Round(trials.Average(trial => trial.FriendlyDeaths), 2),
            Round(trials.Average(trial => trial.RemainingHealthRatio), 4),
            Round(Median(trials.Select(trial => (double)trial.DurationTicks).OrderBy(value => value).ToArray()), 2));
    }

    public EncounterCalibrationEvaluation EvaluateBuilds(EncounterBuildEvaluationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Builds);
        if (request.Builds.Count == 0)
            throw new InvalidOperationException("Encounter build evaluation requires at least one build.");
        if (!double.IsFinite(request.HealthAdjustmentFactor) || request.HealthAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.HealthAdjustmentFactor));
        if (!double.IsFinite(request.DamageAdjustmentFactor) || request.DamageAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.DamageAdjustmentFactor));
        if (request.Simulations is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(request.Simulations));
        if (request.MaxTicks is < 1 or > 100_000)
            throw new ArgumentOutOfRangeException(nameof(request.MaxTicks));

        var definition = towerDefinitions.GetFloors().SingleOrDefault(floor => floor.FloorNumber == request.Floor)
                         ?? throw new InvalidOperationException($"World Tower floor {request.Floor} was not found.");
        if (!creatures.TryGetValue(definition.GuardianCreatureId, out var guardianSource))
            throw new InvalidOperationException($"World Tower guardian '{definition.GuardianCreatureId}' was not found in creatures.json.");
        var calibratedDefinition = WithCalibration(
            definition,
            request.HealthAdjustmentFactor,
            request.DamageAdjustmentFactor);
        var preparedBuilds = request.Builds.Select(PrepareBuild).ToArray();
        var trials = Enumerable.Range(1, request.Simulations)
            .Select(trial => RunTrial(
                calibratedDefinition,
                preparedBuilds,
                guardianSource,
                request.RunSeed,
                trial,
                request.MaxTicks))
            .ToArray();
        return new EncounterCalibrationEvaluation(
            trials.Length,
            Round(trials.Count(trial => trial.Outcome == BattleOutcome.Victory.ToString()) / (double)trials.Length, 4),
            Round(trials.Average(trial => trial.DurationTicks), 2),
            Round(trials.Average(trial => trial.FriendlyDeaths), 2),
            Round(trials.Average(trial => trial.RemainingHealthRatio), 4),
            Round(Median(trials.Select(trial => (double)trial.DurationTicks).OrderBy(value => value).ToArray()), 2));
    }

    private PreparedRepresentativeBuild PrepareBuild(RepresentativeEssenceBuildSnapshot representative)
    {
        var gearDefinition = GearPackageFactory.RegionOneDefinitions.Single(definition =>
            definition.Id.Equals(representative.Character.GearPackageId, StringComparison.Ordinal));
        var canonical = gearPackages.CreateCanonicalBuild(
            gearDefinition,
            representative.Essences.Select(essence => essence.EssenceId).ToArray());
        return new PreparedRepresentativeBuild(
            representative.Id,
            representative.Character.CombatRating.DisplayOverall,
            canonical);
    }

    private PreparedRepresentativeBuild PrepareBuild(EssenceBuildSnapshot build)
    {
        var gearDefinition = GearPackageFactory.RegionOneDefinitions.Single(definition =>
            definition.Id.Equals(build.Character.GearPackageId, StringComparison.Ordinal));
        var canonical = gearPackages.CreateCanonicalBuild(
            gearDefinition,
            build.Essences.Select(essence => essence.EssenceId).ToArray());
        return new PreparedRepresentativeBuild(
            build.Id,
            build.Character.CombatRating.DisplayOverall,
            canonical);
    }

    private WorldTowerTrialSnapshot RunTrial(
        TowerFloorDefinition definition,
        IReadOnlyList<PreparedRepresentativeBuild> builds,
        Creature guardianSource,
        int runSeed,
        int trial,
        int maxTicks)
    {
        var selectionSeed = StableRandom.Seed(
            "balance-world-tower-party-v1",
            runSeed.ToString(CultureInfo.InvariantCulture),
            definition.FloorNumber.ToString(CultureInfo.InvariantCulture),
            trial.ToString(CultureInfo.InvariantCulture));
        var random = new Random(selectionSeed);
        var selected = new List<PreparedRepresentativeBuild>(definition.RequiredSlots);
        while (selected.Count < definition.RequiredSlots)
        {
            var cycle = builds.OrderBy(_ => random.Next()).ToArray();
            selected.AddRange(cycle.Take(definition.RequiredSlots - selected.Count));
        }

        var mappedBuilds = new Dictionary<Guid, PreparedRepresentativeBuild>();
        var friendlyRequests = selected.Select((build, index) =>
        {
            var slotId = $"tower:f{definition.FloorNumber}:t{trial}:player:{index + 1}";
            var snapshotId = StableRandom.Guid("balance-world-tower-snapshot-v1", slotId);
            mappedBuilds.Add(snapshotId, build);
            var snapshot = new CharacterSnapshot
            {
                Id = snapshotId,
                CharacterId = build.Build.Character.Id,
                Name = build.Build.Character.Name,
                Level = build.Build.Character.Level
            };
            return new SnapshotCombatantRequest(
                snapshot,
                new CombatParticipantSlot(slotId, snapshot.CharacterId, CombatSide.Friendly));
        }).ToArray();
        var combatSeed = StableRandom.Seed(
            "balance-world-tower-combat-v1",
            runSeed.ToString(CultureInfo.InvariantCulture),
            definition.FloorNumber.ToString(CultureInfo.InvariantCulture),
            trial.ToString(CultureInfo.InvariantCulture));
        var runtimeFactory = new WorldTowerCombatRuntimeFactory(
            new CombatPreparationPipeline(
                new CalibrationSnapshotCombatantBuilder(mappedBuilds, combatSetup),
                combatSetup));
        var runtime = runtimeFactory.CreateAsync(
                new WorldTowerCombatRuntimeRequest(
                    StableRandom.Guid("balance-world-tower-encounter-v1", combatSeed.ToString(CultureInfo.InvariantCulture)),
                    Guid.Empty,
                    definition,
                    friendlyRequests,
                    CloneCreature(guardianSource),
                    0,
                    0,
                    0,
                    DateTimeOffset.UnixEpoch,
                    combatSeed),
                CancellationToken.None)
            .GetAwaiter().GetResult();
        var result = combatEngine.ExecuteSimulationAsync(
                runtime,
                new CombatRuleset(
                    combatSeed,
                    maxTicks,
                    StartActiveAbilitiesOnCooldown: true,
                    CaptureEventLog: false),
                CancellationToken.None)
            .GetAwaiter().GetResult();
        var maxHealth = result.PlayerTeam.Sum(member => Math.Max(1, member.MaxHealth));
        var currentHealth = result.PlayerTeam.Sum(member => Math.Max(0, member.Health));
        var meanPlayerCr = selected.Average(build => build.DisplayCr);

        return new WorldTowerTrialSnapshot(
            trial,
            combatSeed,
            result.Outcome.ToString(),
            result.Duration,
            result.PlayerTeam.Count(member => member.Health <= 0),
            Round(currentHealth / (double)maxHealth, 4),
            Round(meanPlayerCr, 2),
            selected.Sum(build => build.DisplayCr),
            selected.Select(build => build.Id).ToArray());
    }

    private static RepresentativeEssenceProfileSnapshot SelectProfile(
        IReadOnlyList<RepresentativeEssenceProfileSnapshot> profiles,
        double targetPower) =>
        profiles.OrderBy(profile => Math.Abs(profile.MeanSelectedScore - targetPower))
            .ThenBy(profile => profile.SlotCount)
            .ThenBy(profile => profile.Id, StringComparer.Ordinal)
            .First();

    private static double ResolveAnchorCr(PowerAnchorSuiteSnapshot anchors, string anchorId) =>
        anchors.Anchors.SingleOrDefault(anchor =>
                anchor.Definition.Id.Equals(anchorId, StringComparison.Ordinal))
            ?.CombatRating.MedianDisplayCr
        ?? throw new InvalidOperationException(
            $"Power anchor '{anchorId}' was not found for World Tower CR derivation.");

    private static IReadOnlyList<string> CreateWarnings(
        TowerFloorDefinition definition,
        RepresentativeEssenceProfileSnapshot profile,
        IReadOnlyList<WorldTowerTrialSnapshot> trials,
        int victoryCount,
        double recommendedCr,
        WorldTowerDifficultyClassification classification)
    {
        var warnings = new List<string>();
        if (classification == WorldTowerDifficultyClassification.TooHard)
            warnings.Add("Observed clear rate is below the configured target window.");
        else if (classification == WorldTowerDifficultyClassification.TooEasy)
            warnings.Add("Observed clear rate is above the configured target window.");
        if (victoryCount == 0)
            warnings.Add("No simulated party cleared; the observed clearing-CR threshold is not bounded.");
        else if (victoryCount == trials.Count)
            warnings.Add("Every simulated party cleared; the minimum viable CR threshold is not bounded.");
        var authoredDifference = Math.Abs(recommendedCr - definition.RecommendedPowerRating);
        if (authoredDifference >= Math.Max(10, definition.RecommendedPowerRating * 0.10))
        {
            warnings.Add(FormattableString.Invariant(
                $"Derived CR {Round(recommendedCr, 2):F2} differs materially from authored CR {definition.RecommendedPowerRating}."));
        }
        if (profile.MeanPairwiseSimilarity >= 0.75)
            warnings.Add("Selected representative builds have high Essence similarity; composition coverage is narrow.");
        return warnings;
    }

    private static Creature CloneCreature(Creature source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        ImagePath = source.ImagePath,
        Archetype = source.Archetype,
        DamageProfile = source.DamageProfile,
        DefenseProfile = source.DefenseProfile,
        RewardTableId = source.RewardTableId,
        BaseLevel = source.BaseLevel,
        Level = source.Level,
        Tier = source.Tier,
        StatOverrides = source.StatOverrides.Select(value => new StatOverride
        {
            Id = value.Id,
            AttributeType = value.AttributeType,
            Multiplier = value.Multiplier,
            Additive = value.Additive
        }).ToArray(),
        BaseAttributes = EntityBaseAttributeHelper.CreateEntityAttributes(source.Id)
    };

    private static TowerFloorDefinition WithCalibration(
        TowerFloorDefinition source,
        double healthAdjustment,
        double damageAdjustment) => new()
    {
        FloorNumber = source.FloorNumber,
        Name = source.Name,
        Type = source.Type,
        GuardianCreatureId = source.GuardianCreatureId,
        GuardianName = source.GuardianName,
        GuardianAbilityProfileId = source.GuardianAbilityProfileId,
        RequiredSlots = source.RequiredSlots,
        RecommendedPowerRating = source.RecommendedPowerRating,
        ProgressionPosition = source.ProgressionPosition,
        GuardianScaling = new TowerGuardianScalingDefinition
        {
            Health = checked((float)(source.GuardianScaling.Health * healthAdjustment)),
            Offense = checked((float)(source.GuardianScaling.Offense * damageAdjustment)),
            Defense = source.GuardianScaling.Defense,
            Resistance = source.GuardianScaling.Resistance,
            Penetration = source.GuardianScaling.Penetration,
            Regeneration = source.GuardianScaling.Regeneration
        },
        Stagger = source.Stagger,
        EchoEnabledAfterClear = source.EchoEnabledAfterClear,
        TowerTokens = source.TowerTokens,
        Unlocks = source.Unlocks
    };

    private static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            throw new InvalidOperationException("Median requires at least one value.");
        var middle = values.Count / 2;
        return values.Count % 2 == 1
            ? values[middle]
            : (values[middle - 1] + values[middle]) / 2d;
    }

    private static double Round(double value, int digits) =>
        Math.Round(value, digits, MidpointRounding.AwayFromZero);

    private sealed record PreparedRepresentativeBuild(
        string Id,
        int DisplayCr,
        Services.LL.PowerRatings.CanonicalEquipmentBuild Build);

    private sealed class CalibrationSnapshotCombatantBuilder(
        IReadOnlyDictionary<Guid, PreparedRepresentativeBuild> builds,
        ICombatSetupService combatSetup) : ISnapshotCombatantBuilder
    {
        public Task<IReadOnlyList<CombatRuntimeParticipant>> BuildAsync(
            IReadOnlyList<SnapshotCombatantRequest> requests,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<CombatRuntimeParticipant> participants = requests.Select(request =>
            {
                if (!builds.TryGetValue(request.Snapshot.Id, out var build))
                    throw new InvalidOperationException($"Calibration snapshot '{request.Snapshot.Id}' was not mapped.");
                var combatant = combatSetup.CreatePlayerCombatEntities([build.Build.Character]).Single();
                combatant.EquippedEssences = [.. build.Build.EquippedEssences];
                combatant.HasEquippedEssenceSnapshot = true;
                combatant.Id = request.Slot.SlotId;
                combatant.OriginalId = request.Slot.SourceEntityId;
                return new CombatRuntimeParticipant(request.Slot, build.Build.Character, combatant);
            }).ToArray();
            return Task.FromResult(participants);
        }
    }
}

public static class WorldTowerCreatureCatalog
{
    public static IReadOnlyDictionary<Guid, Creature> Load(string path, JsonSerializerOptions options)
    {
        var document = JsonSerializer.Deserialize<CreatureCatalogDocument>(File.ReadAllText(path), options)
                       ?? throw new InvalidOperationException("Creature catalog is empty or invalid.");
        var creatures = document.Creatures.Select(seed => new Creature
            {
                Id = seed.Id,
                Name = seed.Name,
                ImagePath = seed.ImagePath,
                Archetype = seed.Archetype,
                DamageProfile = seed.DamageProfile,
                DefenseProfile = seed.DefenseProfile,
                RewardTableId = seed.RewardTableId,
                BaseLevel = seed.BaseLevel,
                Level = seed.BaseLevel,
                Tier = seed.Tier,
                StatOverrides = seed.StatOverrides.Select(value => new StatOverride
                {
                    Id = value.Id,
                    AttributeType = value.AttributeType,
                    Multiplier = value.Multiplier,
                    Additive = value.Additive
                }).ToArray(),
                BaseAttributes = EntityBaseAttributeHelper.CreateEntityAttributes(seed.Id)
            })
            .ToArray();
        var duplicate = creatures.GroupBy(creature => creature.Id).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Creature catalog contains duplicate id '{duplicate.Key}'.");
        return creatures.ToDictionary(creature => creature.Id);
    }

    private sealed class CreatureCatalogDocument
    {
        public List<CreatureSeed> Creatures { get; init; } = [];
    }

    private sealed class CreatureSeed
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string ImagePath { get; init; } = string.Empty;
        public CreatureArchetype Archetype { get; init; } = CreatureArchetype.Balanced;
        public DamageProfile DamageProfile { get; init; } = DamageProfile.Hybrid;
        public DefenseProfile DefenseProfile { get; init; } = DefenseProfile.Balanced;
        public string? RewardTableId { get; init; }
        public int BaseLevel { get; init; } = 1;
        public int Tier { get; init; } = 1;
        public List<CreatureStatOverrideSeed> StatOverrides { get; init; } = [];
    }

    private sealed class CreatureStatOverrideSeed
    {
        public Guid Id { get; init; }
        public AttributeType AttributeType { get; init; }
        public float? Multiplier { get; init; }
        public float? Additive { get; init; }
    }
}
