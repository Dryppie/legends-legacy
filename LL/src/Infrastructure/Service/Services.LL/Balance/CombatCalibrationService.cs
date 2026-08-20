using System.Text;
using Application.Interfaces.Services.LL.Balance;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Engine;
using Services.LL.Interfaces;
using Services.LL.PowerRatings;

namespace Services.LL.Balance;

public sealed class CombatCalibrationService : ICombatCalibrationService
{
    private const int MaximumCanonicalEssences = 6;
    private const int DefaultSeed = 104_729;

    private readonly IAreaCombatSimulator _simulator;
    private readonly IAreaRepository _areas;
    private readonly IEntityService _entities;
    private readonly IRegionCreatureScalingProvider _scaling;
    private readonly CanonicalEquipmentBuildFactory _builds;
    private readonly IEssenceSlotUnlockService _essenceSlots;
    private readonly PowerAnalysisSimulationRunner _simulations;
    private readonly ICombatDifficultyEvaluator _evaluator;

    public CombatCalibrationService(
        IAreaCombatSimulator simulator,
        IAreaRepository areas,
        IEntityService entities,
        IRegionCreatureScalingProvider scaling,
        CanonicalEquipmentBuildFactory builds,
        IEssenceSlotUnlockService essenceSlots,
        PowerAnalysisSimulationRunner simulations,
        ICombatDifficultyEvaluator evaluator)
    {
        _simulator = simulator;
        _areas = areas;
        _entities = entities;
        _scaling = scaling;
        _builds = builds;
        _essenceSlots = essenceSlots;
        _simulations = simulations;
        _evaluator = evaluator;
    }

    public async Task<ProgressionCheckpoint> GetCheckpointAsync(
        string areaId,
        CancellationToken cancellationToken)
    {
        var options = await _simulator.GetOptionsAsync(cancellationToken);
        var area = options.Areas.FirstOrDefault(candidate =>
                       candidate.Id.Equals(areaId, StringComparison.OrdinalIgnoreCase))
                   ?? throw new KeyNotFoundException(
                       $"Area '{areaId}' is not part of the calibrated progression catalog.");
        var catalog = _scaling.GetCatalog();
        var regions = catalog.Regions
            .OrderBy(region => region.StartingGlobalStep)
            .ToArray();
        var regionNumber = Array.FindIndex(
                               regions,
                               region => region.RegionKey.Equals(
                                   area.RegionKey,
                                   StringComparison.OrdinalIgnoreCase))
                           + 1;
        var tier = area.DefaultBuildId.Equals(
            CanonicalEquipmentBuildFactory.TutorialStarterBuildId,
            StringComparison.OrdinalIgnoreCase)
            ? 1
            : _builds.GetProgressionLadder().Single(rung =>
                    rung.Id.Equals(area.DefaultBuildId, StringComparison.OrdinalIgnoreCase))
                .Tier;

        return new ProgressionCheckpoint(
            regionNumber,
            area.RegionKey,
            area.RegionStep + 1,
            area.Id,
            area.Name,
            area.GlobalStep,
            area.LevelRequirement,
            tier,
            Math.Min(
                MaximumCanonicalEssences,
                _essenceSlots.GetUnlockedSlotCount(area.LevelRequirement)),
            area.DefaultBuildId,
            area.RecommendedCombatRating,
            catalog.Version);
    }

    public async Task<CalibrationPlayerProfile> CreatePlayerAsync(
        ProgressionCheckpoint checkpoint,
        CalibrationStrengthBand strength,
        CalibrationArchetype archetype,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        var canonicalProfile = MapArchetype(archetype);
        var build = CreateBuild(checkpoint, strength, canonicalProfile);
        var combatant = await _simulations.CreateCanonicalCombatantAsync(build, cancellationToken);

        return new CalibrationPlayerProfile(
            checkpoint,
            strength,
            archetype,
            build.Rung.Id == "t1-standard-common" &&
            build.Equipment.Count == 1 &&
            checkpoint.ExpectedBuildId.Equals(
                CanonicalEquipmentBuildFactory.TutorialStarterBuildId,
                StringComparison.OrdinalIgnoreCase)
                ? CanonicalEquipmentBuildFactory.TutorialStarterBuildId
                : build.Rung.Id,
            build.Rung.Quality.ToString(),
            build.Rung.Rarity.ToString(),
            build.Rung.TemperingSteps,
            build.Equipment.Count,
            build.EquippedEssences.Count,
            build.Rating.Overall / 10,
            build.Rating.SingleTargetOffense / 10,
            build.Rating.MultiTargetOffense / 10,
            build.Rating.PhysicalDurability / 10,
            build.Rating.MagicalDurability / 10,
            build.Rating.Sustain / 10,
            Read(combatant, AttributeType.MaxHealth),
            Read(combatant, AttributeType.Power),
            Read(combatant, AttributeType.Armor),
            Read(combatant, AttributeType.Resistance),
            Read(combatant, AttributeType.AttackSpeed));
    }

    public async Task<AreaCalibrationReport> AnalyzeAreaAsync(
        AreaCalibrationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var checkpoint = await GetCheckpointAsync(request.AreaId, cancellationToken);
        var area = await _areas.GetAreaByIdAsync(request.AreaId)
                   ?? throw new KeyNotFoundException($"Area '{request.AreaId}' was not found.");
        var creatureIds = area.Creatures
            .Select(creature => creature.CreatureId)
            .Distinct()
            .ToArray();
        var creatures = (await _entities.GetEntitiesByIdsForCombatAsync(
                creatureIds.ToList(),
                cancellationToken))
            .OfType<Creature>()
            .ToDictionary(creature => creature.Id);
        if (creatures.Count != creatureIds.Length)
            throw new InvalidOperationException($"Could not load every creature in area '{area.Id}'.");

        var bands = DistinctOrAll(request.StrengthBands);
        var archetypes = DistinctOrAll(request.Archetypes);
        var simulations = Math.Clamp(request.SimulationsPerEncounter, 3, 1_000);
        var baseSeed = request.RandomSeed == 0 ? DefaultSeed : request.RandomSeed;
        var results = new List<AreaCalibrationEncounterResult>(
            creatureIds.Length * bands.Count * archetypes.Count);

        foreach (var strength in bands)
        {
            foreach (var archetype in archetypes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var player = await CreatePlayerAsync(
                    checkpoint,
                    strength,
                    archetype,
                    cancellationToken);
                for (var creatureIndex = 0; creatureIndex < creatureIds.Length; creatureIndex++)
                {
                    var creatureId = creatureIds[creatureIndex];
                    var seed = unchecked(
                        baseSeed
                        + checkpoint.GlobalStep * 104_729
                        + creatureIndex * 15_485_867
                        + (int)strength * 32_452_843
                        + (int)archetype * 49_979_687);
                    var simulation = await _simulator.RunEncounterAsync(
                        new AreaEncounterSimulationRequest(
                            checkpoint.AreaId,
                            creatureId,
                            simulations,
                            seed,
                            MapArchetype(archetype).ToString(),
                            player.BuildId),
                        cancellationToken);
                    var metrics = CreateMetrics(simulation);
                    var creature = creatures[creatureId];
                    results.Add(new AreaCalibrationEncounterResult(
                        creature.Id,
                        creature.Name,
                        creature.Archetype.ToString(),
                        creature.DamageProfile.ToString(),
                        creature.DefenseProfile.ToString(),
                        player,
                        metrics,
                        _evaluator.Evaluate(request.EncounterType, strength, metrics)));
                }
            }
        }

        var outliers = results
            .Where(result => result.Assessment.Status != CalibrationStatus.WithinTarget)
            .Select(result =>
                $"{result.CreatureName} ({result.Player.Strength}/{result.Player.Archetype}): " +
                $"{result.Assessment.Status}; {string.Join(" ", result.Assessment.Diagnostics)}")
            .ToArray();
        var textReport = FormatAreaReport(checkpoint, results, outliers);
        return new AreaCalibrationReport(
            checkpoint,
            simulations,
            baseSeed,
            results,
            outliers,
            textReport);
    }

    public async Task<ProgressionCurveReport> CreateProgressionReportAsync(
        string regionKey,
        CalibrationArchetype archetype,
        CancellationToken cancellationToken)
    {
        var options = await _simulator.GetOptionsAsync(cancellationToken);
        var areaOptions = options.Areas
            .Where(area => area.RegionKey.Equals(regionKey, StringComparison.OrdinalIgnoreCase))
            .OrderBy(area => area.GlobalStep)
            .ToArray();
        if (areaOptions.Length == 0)
            throw new KeyNotFoundException($"Region '{regionKey}' is not calibrated.");

        var points = new List<ProgressionCurvePoint>(areaOptions.Length);
        ProgressionCurvePoint? previous = null;
        foreach (var areaOption in areaOptions)
        {
            var checkpoint = await GetCheckpointAsync(areaOption.Id, cancellationToken);
            var player = await CreatePlayerAsync(
                checkpoint,
                CalibrationStrengthBand.Expected,
                archetype,
                cancellationToken);
            var area = await _areas.GetAreaByIdAsync(areaOption.Id)
                       ?? throw new KeyNotFoundException($"Area '{areaOption.Id}' was not found.");
            var enemy = _scaling.GetScaling(area);
            var point = new ProgressionCurvePoint(
                checkpoint,
                player,
                enemy.HealthMultiplier,
                enemy.OffenseMultiplier,
                enemy.DefenseMultiplier,
                enemy.ResistanceMultiplier,
                PercentIncrease(previous?.ExpectedPlayer.CombatRating, player.CombatRating),
                PercentIncrease(previous?.EnemyHealthMultiplier, enemy.HealthMultiplier),
                PercentIncrease(previous?.EnemyOffenseMultiplier, enemy.OffenseMultiplier));
            points.Add(point);
            previous = point;
        }

        var warnings = new List<string>();
        foreach (var (before, after) in points.Zip(points.Skip(1)))
        {
            if (after.ExpectedPlayer.CombatRating < before.ExpectedPlayer.CombatRating)
            {
                warnings.Add(
                    $"{after.Checkpoint.AreaId}: expected player Combat Rating decreases from " +
                    $"{before.ExpectedPlayer.CombatRating} to {after.ExpectedPlayer.CombatRating}.");
            }
            if (after.EnemyHealthMultiplier < before.EnemyHealthMultiplier ||
                after.EnemyOffenseMultiplier < before.EnemyOffenseMultiplier)
            {
                warnings.Add($"{after.Checkpoint.AreaId}: enemy baseline scaling moves backwards.");
            }
            if (after.PlayerCombatRatingIncreasePercent > 35)
            {
                warnings.Add(
                    $"{after.Checkpoint.AreaId}: expected player Combat Rating jumps by " +
                    $"{after.PlayerCombatRatingIncreasePercent:N1}%.");
            }
        }

        return new ProgressionCurveReport(
            regionKey,
            archetype,
            points,
            warnings,
            FormatProgressionReport(regionKey, archetype, points, warnings));
    }

    private CanonicalEquipmentBuild CreateBuild(
        ProgressionCheckpoint checkpoint,
        CalibrationStrengthBand strength,
        CanonicalPartyProfile profile)
    {
        if (checkpoint.ExpectedBuildId.Equals(
                CanonicalEquipmentBuildFactory.TutorialStarterBuildId,
                StringComparison.OrdinalIgnoreCase) &&
            strength is CalibrationStrengthBand.Undergeared or CalibrationStrengthBand.Expected)
        {
            return _builds.CreateTutorialStarterBuild();
        }

        var ladder = _builds.GetProgressionLadder();
        var expectedIndex = checkpoint.ExpectedBuildId.Equals(
            CanonicalEquipmentBuildFactory.TutorialStarterBuildId,
            StringComparison.OrdinalIgnoreCase)
            ? ladder.Single(rung => rung.Id == "t1-standard-common").Index
            : ladder.Single(rung =>
                    rung.Id.Equals(checkpoint.ExpectedBuildId, StringComparison.OrdinalIgnoreCase))
                .Index;
        var maximumAvailableIndex = ladder
            .Where(rung => rung.Tier <= checkpoint.EquipmentTier)
            .Max(rung => rung.Index);
        var rung = ladder[CalibrationStrengthBandPolicy.ResolveRungIndex(
            expectedIndex,
            maximumAvailableIndex,
            strength)];
        return _builds.CreateBuildForArea(
            profile,
            rung,
            checkpoint.CharacterLevel,
            checkpoint.EssenceSlots);
    }

    private static CombatCalibrationMetrics CreateMetrics(AreaEncounterSimulationReport report)
    {
        var attempts = report.Attempts;
        var wins = attempts.Count(attempt =>
            attempt.Outcome.Equals(BattleOutcome.Victory.ToString(), StringComparison.OrdinalIgnoreCase));
        var durationSeconds = attempts
            .Select(attempt => attempt.CombatTicks / (double)FastCombatEngine.TicksPerSecond)
            .ToArray();
        var healthLost = attempts
            .Select(attempt => report.PlayerMaxHealth <= 0
                ? 100d
                : Math.Clamp(
                    (report.PlayerMaxHealth - attempt.RemainingHealth) * 100d / report.PlayerMaxHealth,
                    0d,
                    100d))
            .ToArray();
        var totalSeconds = durationSeconds.Sum();
        return new CombatCalibrationMetrics(
            attempts.Count,
            Math.Round(wins * 100d / attempts.Count, 2),
            Math.Round((attempts.Count - wins) * 100d / attempts.Count, 2),
            Math.Round(durationSeconds.Average(), 2),
            Percentile(durationSeconds, 0.50),
            Percentile(durationSeconds, 0.95),
            Math.Round(healthLost.Average(), 2),
            Percentile(healthLost, 0.50),
            Percentile(healthLost, 0.95),
            Math.Round(100d - healthLost.Average(), 2),
            totalSeconds <= 0 ? 0 : Math.Round(wins * 60d / totalSeconds, 2),
            Math.Round(attempts.Average(attempt => attempt.DamageTaken), 2),
            Math.Round(attempts.Average(attempt => attempt.HealingDone), 2),
            Math.Round(attempts.Average(attempt => attempt.HealthRegenerated), 2));
    }

    private static string FormatAreaReport(
        ProgressionCheckpoint checkpoint,
        IReadOnlyList<AreaCalibrationEncounterResult> results,
        IReadOnlyList<string> outliers)
    {
        var text = new StringBuilder()
            .AppendLine($"Region {checkpoint.RegionNumber} / Area {checkpoint.AreaNumber} - {checkpoint.AreaName}")
            .AppendLine($"Checkpoint: level {checkpoint.CharacterLevel}, expected build {checkpoint.ExpectedBuildId}, CR {checkpoint.RecommendedCombatRating}")
            .AppendLine()
            .AppendLine("Creature | Strength / Archetype | Win | Median TTK | Median HP Lost | Kills/min | Assessment")
            .AppendLine("--- | --- | ---: | ---: | ---: | ---: | ---");
        foreach (var result in results
                     .OrderBy(result => result.CreatureName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(result => result.Player.Strength)
                     .ThenBy(result => result.Player.Archetype))
        {
            text.AppendLine(
                $"{result.CreatureName} | {result.Player.Strength} / {result.Player.Archetype} | " +
                $"{result.Metrics.WinRatePercent:N1}% | {result.Metrics.MedianDurationSeconds:N1}s | " +
                $"{result.Metrics.MedianHealthLostPercent:N1}% | {result.Metrics.KillsPerMinute:N2} | " +
                result.Assessment.Status);
        }
        text.AppendLine().AppendLine($"Outliers: {outliers.Count}");
        foreach (var outlier in outliers)
            text.AppendLine($"- {outlier}");
        return text.ToString();
    }

    private static string FormatProgressionReport(
        string regionKey,
        CalibrationArchetype archetype,
        IReadOnlyList<ProgressionCurvePoint> points,
        IReadOnlyList<string> warnings)
    {
        var text = new StringBuilder()
            .AppendLine($"Progression curve: {regionKey} / {archetype}")
            .AppendLine("Checkpoint | Level | Build | CR | Player CR Δ | Enemy HP | Enemy HP Δ | Enemy Offense | Enemy Offense Δ")
            .AppendLine("--- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---:");
        foreach (var point in points)
        {
            text.AppendLine(
                $"R{point.Checkpoint.RegionNumber}A{point.Checkpoint.AreaNumber} | " +
                $"{point.Checkpoint.CharacterLevel} | {point.ExpectedPlayer.BuildId} | " +
                $"{point.ExpectedPlayer.CombatRating} | {point.PlayerCombatRatingIncreasePercent:N1}% | " +
                $"{point.EnemyHealthMultiplier:N2} | {point.EnemyHealthIncreasePercent:N1}% | " +
                $"{point.EnemyOffenseMultiplier:N2} | {point.EnemyOffenseIncreasePercent:N1}%");
        }
        if (warnings.Count > 0)
        {
            text.AppendLine().AppendLine("Warnings:");
            foreach (var warning in warnings)
                text.AppendLine($"- {warning}");
        }
        return text.ToString();
    }

    private static IReadOnlyList<T> DistinctOrAll<T>(IReadOnlyList<T>? values)
        where T : struct, Enum =>
        values is { Count: > 0 }
            ? values.Distinct().ToArray()
            : Enum.GetValues<T>();

    private static CanonicalPartyProfile MapArchetype(CalibrationArchetype archetype) =>
        archetype switch
        {
            CalibrationArchetype.Balanced => CanonicalPartyProfile.Balanced,
            CalibrationArchetype.Offensive => CanonicalPartyProfile.Offense,
            CalibrationArchetype.Defensive => CanonicalPartyProfile.Defensive,
            CalibrationArchetype.Sustain => CanonicalPartyProfile.Sustain,
            CalibrationArchetype.AreaDamage => CanonicalPartyProfile.Area,
            _ => throw new ArgumentOutOfRangeException(nameof(archetype), archetype, null)
        };

    private static int Read(Domain.Models.Combat.CombatEntity combatant, AttributeType type) =>
        (int)Math.Round((double)combatant.GetAttributeValue(type));

    private static double PercentIncrease(double? previous, double current) =>
        previous is null || previous <= 0
            ? 0
            : Math.Round((current / previous.Value - 1d) * 100d, 2);

    private static double Percentile(IEnumerable<double> values, double percentile)
    {
        var ordered = values.Order().ToArray();
        if (ordered.Length == 0)
            return 0;
        var position = (ordered.Length - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper)
            return Math.Round(ordered[lower], 2);
        return Math.Round(
            ordered[lower] + (ordered[upper] - ordered[lower]) * (position - lower),
            2);
    }
}
