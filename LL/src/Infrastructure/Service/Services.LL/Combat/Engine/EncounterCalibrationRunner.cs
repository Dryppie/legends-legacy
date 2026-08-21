using System.Text.Json;
using Application.Interfaces.Services.LL.Combat;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates;
using Domain.Models.Entities.Creatures.Templates.Enums;
using Domain.Models.Essences;
using Domain.Models.Raids;
using Domain.Models.Regions.Areas;
using Domain.Models.WorldTower;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Layers.Resolution.Dungeon;
using Services.LL.Entities.Creatures;
using Services.LL.WorldTower;

namespace Services.LL.Combat.Engine;

/// <summary>
/// Resolves a small, authored sample of Idle, Dungeon, and Tower encounters
/// into the same attributes, rosters, ability profiles, and content multipliers
/// used by their live combat paths.
/// </summary>
public sealed class AuthoredEncounterCalibrationFactory
{
    public const string DefaultManifestFileName = "encounter-calibration-samples.json";

    private readonly EncounterCalibrationManifest _manifest;
    private readonly IReadOnlyDictionary<Guid, CreatureContentDefinition> _creaturesById;
    private readonly IReadOnlyDictionary<string, CreatureContentDefinition> _creaturesByMonsterId;
    private readonly IReadOnlyDictionary<string, AreaContentDefinition> _areas;
    private readonly IReadOnlyDictionary<string, DungeonFamilyContentDefinition> _dungeons;
    private readonly IReadOnlyDictionary<int, TowerFloorDefinition> _towerFloors;
    private readonly IReadOnlyDictionary<string, RaidBossDefinition> _raidBosses;
    private readonly IReadOnlyDictionary<string, EncounterCalibrationPartyComposition> _partyCompositions;
    private readonly CreatureScaler _creatureScaler;
    private readonly ICreatureAbilityDefinitionProvider _creatureAbilities;

    public AuthoredEncounterCalibrationFactory(
        IConfiguration configuration,
        string contentRootPath,
        JsonSerializerOptions jsonOptions,
        IRegionCreatureScalingProvider scalingProvider,
        ICreatureAbilityDefinitionProvider creatureAbilities)
    {
        var dataRoot = Path.Combine(
            contentRootPath,
            configuration["Content:Root"] ?? "Data");
        _manifest = Read<EncounterCalibrationManifest>(
            Path.Combine(dataRoot, "progression", DefaultManifestFileName), jsonOptions);
        var creatures = Read<CreatureContentDocument>(
            Path.Combine(dataRoot, "world", "creatures.json"), jsonOptions).Creatures;
        _creaturesById = creatures.ToDictionary(creature => creature.Id);
        _creaturesByMonsterId = creatures.ToDictionary(
            creature => CreatureEssenceSource.GetMonsterDefinitionId(creature.Name),
            StringComparer.OrdinalIgnoreCase);
        _areas = Read<RegionContentDocument>(
                Path.Combine(dataRoot, "world", "regions.json"), jsonOptions)
            .Regions.SelectMany(region => region.Areas)
            .ToDictionary(area => area.Id, StringComparer.OrdinalIgnoreCase);
        _dungeons = Read<DungeonContentDocument>(
                Path.Combine(dataRoot, "dungeons", "dungeons.json"), jsonOptions)
            .Families.ToDictionary(family => family.Id, StringComparer.OrdinalIgnoreCase);
        _towerFloors = Read<WorldTowerCatalogDocument>(
                Path.Combine(dataRoot, "world-tower", "tower-floors.json"), jsonOptions)
            .Floors.ToDictionary(floor => floor.FloorNumber);
        _raidBosses = Read<RaidBossCatalogDocument>(
                Path.Combine(dataRoot, "raids", "raid-bosses.json"), jsonOptions)
            .RaidBosses.ToDictionary(boss => boss.Id, StringComparer.OrdinalIgnoreCase);
        _partyCompositions = _manifest.PartyCompositions.ToDictionary(
            composition => composition.Id,
            StringComparer.OrdinalIgnoreCase);
        _creatureScaler = new CreatureScaler(scalingProvider);
        _creatureAbilities = creatureAbilities;
        ValidateManifest();
    }

    public EncounterCalibrationCatalog CreateCatalog()
    {
        var bands = _manifest.Bands.ToDictionary(band => band.Id, StringComparer.OrdinalIgnoreCase);
        var encounters = _manifest.Samples.Select(sample => sample.ContentType switch
        {
            EncounterCalibrationContentType.Idle => CreateIdle(sample, bands[sample.BandId]),
            EncounterCalibrationContentType.Dungeon => CreateDungeon(sample, bands[sample.BandId]),
            EncounterCalibrationContentType.Tower => CreateTower(sample, bands[sample.BandId]),
            EncounterCalibrationContentType.Raid => CreateRaid(sample, bands[sample.BandId]),
            _ => throw new InvalidOperationException($"Unsupported encounter content type '{sample.ContentType}'.")
        }).ToList();

        return new EncounterCalibrationCatalog(
            _manifest.Version,
            _manifest.AssessmentGearEnvelopeId,
            _manifest.AssessmentEssenceEnvelopeId,
            _manifest.MaximumBuildWinRateSpread,
            _manifest.SupportAssessment,
            _manifest.CompositionAssessment,
            _manifest.PartyCompositions,
            encounters);
    }

    private AuthoredEncounterCalibrationScenario CreateIdle(
        EncounterCalibrationSample sample,
        EncounterCalibrationBand band)
    {
        var area = _areas[sample.AreaId];
        var areaCreatureIds = area.Creatures.Select(creature => creature.CreatureId).ToHashSet();
        var hostiles = sample.CreatureIds.Select((creatureId, index) =>
        {
            if (!areaCreatureIds.Contains(creatureId))
            {
                throw new InvalidOperationException(
                    $"Encounter '{sample.Id}' references creature '{creatureId}' outside area '{area.Id}'.");
            }

            return CreateHostile(
                _creaturesById[creatureId],
                area.Id,
                area.DifficultyTier,
                index,
                static _ => { });
        }).ToList();

        return MapScenario(sample, band, hostiles, playerCount: 1, area.DifficultyTier, []);
    }

    private AuthoredEncounterCalibrationScenario CreateDungeon(
        EncounterCalibrationSample sample,
        EncounterCalibrationBand band)
    {
        var family = _dungeons[sample.DungeonFamilyId];
        var difficulty = family.Difficulties.Single(candidate =>
            candidate.Difficulty == sample.DungeonDifficulty);
        var template = family.RoomTemplates.Single(candidate =>
            candidate.Id.Equals(sample.RoomTemplateId, StringComparison.OrdinalIgnoreCase));
        var progressionPosition = DungeonEnemyDifficultyScaling.GetProgressionPosition(
            sample.DungeonDifficulty,
            family.Region);
        var strengthMultiplier = DungeonEnemyDifficultyScaling.GetStrengthMultiplier(
            sample.DungeonDifficulty,
            difficulty.EnemyStrengthMultiplier);
        var hostiles = template.EncounterIds.Select((monsterKey, index) =>
        {
            var monsterId = monsterKey.StartsWith("monster.", StringComparison.OrdinalIgnoreCase)
                ? monsterKey
                : $"monster.{monsterKey}";
            if (!_creaturesByMonsterId.TryGetValue(monsterId, out var creature))
                throw new InvalidOperationException($"Encounter '{sample.Id}' references unknown monster '{monsterId}'.");

            return CreateHostile(
                creature,
                string.Empty,
                progressionPosition,
                index,
                attributes => ApplyMultiplier(attributes, strengthMultiplier));
        }).ToList();

        return MapScenario(sample, band, hostiles, playerCount: 1, progressionPosition, []);
    }

    private AuthoredEncounterCalibrationScenario CreateTower(
        EncounterCalibrationSample sample,
        EncounterCalibrationBand band)
    {
        var floor = _towerFloors[sample.TowerFloorNumber];
        var hostiles = new[]
        {
            CreateHostile(
                _creaturesById[floor.GuardianCreatureId],
                string.Empty,
                floor.ProgressionPosition,
                0,
                attributes => ApplyTowerScaling(attributes, floor),
                floor.GuardianAbilityProfileId,
                floor.Stagger,
                floor.RequiredSlots)
        };

        return MapScenario(
            sample,
            band,
            hostiles,
            floor.RequiredSlots,
            floor.ProgressionPosition,
            sample.PartyCompositionIds.Select(id => _partyCompositions[id]).ToList());
    }

    private AuthoredEncounterCalibrationScenario CreateRaid(
        EncounterCalibrationSample sample,
        EncounterCalibrationBand band)
    {
        var raid = _raidBosses[sample.RaidBossId];
        var tier = sample.RaidPlusLevel.HasValue
            ? RaidPlusDifficulty.Create(raid, sample.RaidPlusLevel.Value)
            : raid.Tiers.Single(candidate => candidate.Tier == sample.RaidTier);
        var preparation = Math.Clamp(sample.RaidPreparationPercent / 100m, 0m, 1m);
        var hostile = CreateHostile(
            _creaturesById[tier.Boss.CreatureId],
            string.Empty,
            progressionPosition: 1,
            index: 0,
            attributes =>
            {
                ApplyRaidScaling(attributes, tier.Boss.Scaling);
                ApplyPercent(
                    attributes,
                    AttributeType.Armor,
                    -(preparation * tier.Boss.MaxGuardianBreakPercent));
                ApplyPercent(
                    attributes,
                    AttributeType.Resistance,
                    -(preparation * tier.Boss.MaxGuardianBreakPercent));
                ApplyPercent(
                    attributes,
                    AttributeType.DamageReduction,
                    -(preparation * tier.Boss.MaxGuardianBreakPercent));
                ApplyPercent(
                    attributes,
                    AttributeType.Power,
                    -(preparation * tier.Boss.MaxSignaturePowerReductionPercent));
            },
            staggerDefinition: tier.Boss.Stagger,
            staggerParticipantCount: tier.MinimumRoster,
            abilityCooldownDelayFraction: (double)(
                preparation * tier.Boss.MaxSignatureCooldownDelayPercent / 100m));

        return new AuthoredEncounterCalibrationScenario(
            sample.Id,
            sample.ContentType,
            sample.DifficultyRole,
            sample.SnapshotAnchorId,
            sample.ProgressionPosition,
            tier.MinimumRoster,
            sample.MaxTicks,
            band,
            [hostile],
            sample.PartyCompositionIds.Select(id => _partyCompositions[id]).ToList(),
            sample.CompositionExpectations,
            tier.Boss.OvertimeStartsAtTick,
            300,
            tier.Boss.OvertimePowerIncreasePercent);
    }

    private EncounterCalibrationHostile CreateHostile(
        CreatureContentDefinition definition,
        string areaId,
        int progressionPosition,
        int index,
        Action<Dictionary<AttributeType, float>> applyContentScaling,
        string? monsterIdOverride = null,
        BossStaggerDefinition? staggerDefinition = null,
        int staggerParticipantCount = 1,
        double abilityCooldownDelayFraction = 0d)
    {
        var creature = new Creature
        {
            Id = definition.Id,
            Name = definition.Name,
            ImagePath = definition.ImagePath,
            Archetype = definition.Archetype,
            DamageProfile = definition.DamageProfile,
            DefenseProfile = definition.DefenseProfile,
            BaseLevel = definition.BaseLevel,
            Level = definition.BaseLevel,
            Tier = definition.Tier,
            StatOverrides = definition.StatOverrides
        };
        _creatureScaler.ApplyScaling(
            creature,
            new Area { Id = areaId, DifficultyTier = progressionPosition });
        var attributes = new Dictionary<AttributeType, float>(creature.BaseAttributesDict);
        applyContentScaling(attributes);
        var monsterId = string.IsNullOrWhiteSpace(monsterIdOverride)
            ? CreatureEssenceSource.GetMonsterDefinitionId(definition.Name)
            : monsterIdOverride;

        return new EncounterCalibrationHostile(
            $"{monsterId}.{index + 1}",
            monsterId,
            definition.Name,
            WithRequiredAttributes(attributes),
            _creatureAbilities.GetAbilityIds(monsterId),
            staggerDefinition,
            staggerParticipantCount,
            abilityCooldownDelayFraction);
    }

    private static AuthoredEncounterCalibrationScenario MapScenario(
        EncounterCalibrationSample sample,
        EncounterCalibrationBand band,
        IReadOnlyList<EncounterCalibrationHostile> hostiles,
        int playerCount,
        int progressionPosition,
        IReadOnlyList<EncounterCalibrationPartyComposition> partyCompositions) => new(
        sample.Id,
        sample.ContentType,
        sample.DifficultyRole,
        sample.SnapshotAnchorId,
        progressionPosition,
        playerCount,
        sample.MaxTicks,
        band,
        hostiles,
        partyCompositions,
        sample.CompositionExpectations);

    private static void ApplyMultiplier(Dictionary<AttributeType, float> attributes, float multiplier)
    {
        foreach (var attribute in DungeonScaledAttributes)
            attributes[attribute] = attributes.GetValueOrDefault(attribute) * multiplier;
    }

    private static void ApplyTowerScaling(
        Dictionary<AttributeType, float> attributes,
        TowerFloorDefinition floor)
    {
        var participantHealth = MathF.Pow(floor.RequiredSlots, 0.85f);
        var participantOffense = 1f + 0.05f * (floor.RequiredSlots - 1);
        var participantDurability = MathF.Pow(floor.RequiredSlots / 5f, 0.25f);
        Multiply(attributes, AttributeType.MaxHealth,
            WorldTowerGuardianScaling.HealthContentMultiplier * participantHealth * floor.GuardianScaling.Health);
        Multiply(attributes, AttributeType.Power,
            WorldTowerGuardianScaling.OffenseContentMultiplier * participantOffense * floor.GuardianScaling.Offense);
        Multiply(attributes, AttributeType.Armor,
            WorldTowerGuardianScaling.DurabilityContentMultiplier * participantDurability * floor.GuardianScaling.Defense);
        Multiply(attributes, AttributeType.Resistance,
            WorldTowerGuardianScaling.DurabilityContentMultiplier * participantDurability * floor.GuardianScaling.Resistance);
        Multiply(attributes, AttributeType.ArmorPenetration, floor.GuardianScaling.Penetration);
        Multiply(attributes, AttributeType.MagicPenetration, floor.GuardianScaling.Penetration);
        Multiply(attributes, AttributeType.HealthRegeneration, floor.GuardianScaling.Regeneration);
    }

    private static void ApplyRaidScaling(
        IDictionary<AttributeType, float> attributes,
        RaidAttributeScalingDefinition scaling)
    {
        Multiply(attributes, AttributeType.MaxHealth, scaling.Health);
        Multiply(attributes, AttributeType.Power, scaling.Offense);
        Multiply(attributes, AttributeType.Armor, scaling.Defense);
        Multiply(attributes, AttributeType.Resistance, scaling.Resistance);
        Multiply(attributes, AttributeType.ArmorPenetration, scaling.Penetration);
        Multiply(attributes, AttributeType.MagicPenetration, scaling.Penetration);
        Multiply(attributes, AttributeType.HealthRegeneration, scaling.Regeneration);
    }

    private static void ApplyPercent(
        IDictionary<AttributeType, float> attributes,
        AttributeType type,
        decimal percent)
    {
        attributes.TryGetValue(type, out var value);
        attributes[type] = value * (1f + (float)percent / 100f);
    }

    private static void Multiply(
        IDictionary<AttributeType, float> attributes,
        AttributeType type,
        float multiplier)
    {
        attributes.TryGetValue(type, out var value);
        attributes[type] = value * multiplier;
    }

    private void ValidateManifest()
    {
        if (_manifest.Version <= 0
            || string.IsNullOrWhiteSpace(_manifest.AssessmentGearEnvelopeId)
            || string.IsNullOrWhiteSpace(_manifest.AssessmentEssenceEnvelopeId)
            || _manifest.MaximumBuildWinRateSpread is < 0 or > 1
            || _manifest.SupportAssessment is null
            || string.IsNullOrWhiteSpace(_manifest.SupportAssessment.BaselineCompositionId)
            || string.IsNullOrWhiteSpace(_manifest.SupportAssessment.SupportCompositionId)
            || _manifest.SupportAssessment.BaselineCompositionId.Equals(
                _manifest.SupportAssessment.SupportCompositionId,
                StringComparison.OrdinalIgnoreCase)
            || _manifest.SupportAssessment.MinimumDeathRateReduction is < 0 or > 1
            || _manifest.SupportAssessment.MinimumSurvivalResourceIncreasePercent < 0
            || _manifest.SupportAssessment.MinimumFirstDeathDelayTicks < 0
            || _manifest.SupportAssessment.MaximumDurationIncreaseRate < 0
            || _manifest.CompositionAssessment is null
            || _manifest.CompositionAssessment.AlternativeMinimumWinRate is < 0 or > 1
            || _manifest.CompositionAssessment.AlternativeMaximumTimeoutRate is < 0 or > 1
            || _manifest.CompositionAssessment.CounteredMaximumWinRate is < 0 or > 1
            || _manifest.CompositionAssessment.ChallengeMaximumWinRate is < 0 or > 1
            || _manifest.PartyCompositions.Count == 0
            || _manifest.Bands.Count == 0
            || _manifest.Samples.Count == 0)
        {
            throw new InvalidOperationException("Encounter calibration manifest contains invalid global assumptions.");
        }

        ThrowForDuplicateIds(_manifest.Bands.Select(band => band.Id), "band");
        ThrowForDuplicateIds(_manifest.Samples.Select(sample => sample.Id), "sample");
        ThrowForDuplicateIds(_manifest.PartyCompositions.Select(composition => composition.Id), "party composition");
        if (!_partyCompositions.ContainsKey(_manifest.SupportAssessment.BaselineCompositionId)
            || !_partyCompositions.ContainsKey(_manifest.SupportAssessment.SupportCompositionId))
        {
            throw new InvalidOperationException(
                "Encounter calibration support assessment references an unknown party composition.");
        }
        foreach (var composition in _manifest.PartyCompositions)
        {
            if (composition.MemberBuildFamilyIds.Count != WorldTowerPartyRules.MaximumPartySize
                || composition.MemberBuildFamilyIds.Any(string.IsNullOrWhiteSpace)
                || composition.RosterOverrides is not null
                && (composition.RosterOverrides
                        .GroupBy(roster => roster.PlayerCount)
                        .Any(group => group.Key <= 0 || group.Count() > 1)
                    || composition.RosterOverrides.Any(roster =>
                        roster.MemberBuildFamilyIds.Count != roster.PlayerCount
                        || roster.MemberBuildFamilyIds.Any(string.IsNullOrWhiteSpace))))
            {
                throw new InvalidOperationException(
                    $"Party composition '{composition.Id}' has an invalid base pattern or roster override.");
            }
        }
        var bandIds = _manifest.Bands.Select(band => band.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var band in _manifest.Bands)
        {
            if (band.MinimumWinRate is < 0 or > 1
                || band.MaximumWinRate is < 0 or > 1
                || band.MinimumWinRate > band.MaximumWinRate
                || band.MaximumTimeoutRate is < 0 or > 1
                || band.MinimumDurationTicks < 0
                || band.MaximumDurationTicks < band.MinimumDurationTicks
                || band.MinimumSurvivalResourcePercent is < 0 or > 100
                || (band.AssessedBuildFamilyIds is not null
                    && (band.AssessedBuildFamilyIds.Count == 0
                        || band.AssessedBuildFamilyIds.Any(string.IsNullOrWhiteSpace)
                        || band.AssessedBuildFamilyIds.Distinct(StringComparer.OrdinalIgnoreCase).Count()
                        != band.AssessedBuildFamilyIds.Count))
                || (band.Stagger is not null
                    && (band.Stagger.MinimumAverageBreaks < 0
                        || band.Stagger.MaximumAverageBreaks < band.Stagger.MinimumAverageBreaks
                        || band.Stagger.MaximumAverageFirstBreakTick <= 0
                        || band.Stagger.MaximumUptimePercent is < 0 or > 100
                        || band.Stagger.MaximumBreakCapRate is < 0 or > 1)))
            {
                throw new InvalidOperationException($"Encounter calibration band '{band.Id}' is invalid.");
            }
        }

        foreach (var sample in _manifest.Samples)
        {
            if (string.IsNullOrWhiteSpace(sample.SnapshotAnchorId)
                || !bandIds.Contains(sample.BandId)
                || sample.MaxTicks <= 0)
            {
                throw new InvalidOperationException($"Encounter calibration sample '{sample.Id}' is incomplete.");
            }

            switch (sample.ContentType)
            {
                case EncounterCalibrationContentType.Idle:
                    if (!_areas.ContainsKey(sample.AreaId)
                        || sample.CreatureIds.Count == 0
                        || sample.CompositionExpectations.Count > 0)
                        throw new InvalidOperationException($"Idle sample '{sample.Id}' has invalid source references.");
                    break;
                case EncounterCalibrationContentType.Dungeon:
                    if (!_dungeons.ContainsKey(sample.DungeonFamilyId)
                        || sample.DungeonDifficulty is < 1 or > 3
                        || string.IsNullOrWhiteSpace(sample.RoomTemplateId)
                        || sample.CompositionExpectations.Count > 0)
                    {
                        throw new InvalidOperationException($"Dungeon sample '{sample.Id}' has invalid source references.");
                    }
                    break;
                case EncounterCalibrationContentType.Tower:
                    if (!_towerFloors.ContainsKey(sample.TowerFloorNumber)
                        || sample.PartyCompositionIds.Count == 0
                        || sample.PartyCompositionIds.Any(id => !_partyCompositions.ContainsKey(id))
                        || !HasValidCompositionExpectations(sample))
                        throw new InvalidOperationException($"Tower sample '{sample.Id}' has invalid source references.");
                    break;
                case EncounterCalibrationContentType.Raid:
                    if (!_raidBosses.TryGetValue(sample.RaidBossId, out var raid)
                        || sample.ProgressionPosition <= 0
                        || sample.PartyCompositionIds.Count == 0
                        || sample.PartyCompositionIds.Any(id => !_partyCompositions.ContainsKey(id))
                        || !HasValidCompositionExpectations(sample)
                        || sample.RaidPreparationPercent is < 0 or > 100
                        || (sample.RaidPlusLevel.HasValue
                            ? sample.RaidPlusLevel.Value <= 0
                            : raid.Tiers.All(tier => tier.Tier != sample.RaidTier)))
                    {
                        throw new InvalidOperationException($"Raid sample '{sample.Id}' has invalid source references.");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Sample '{sample.Id}' has an invalid content type.");
            }
        }
    }

    private static void ThrowForDuplicateIds(IEnumerable<string> ids, string label)
    {
        var duplicate = ids.GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Invalid or duplicate encounter calibration {label} id '{duplicate.Key}'.");
    }

    private static bool HasValidCompositionExpectations(EncounterCalibrationSample sample)
    {
        if (sample.CompositionExpectations is null
            || sample.PartyCompositionIds.Distinct(StringComparer.OrdinalIgnoreCase).Count()
            != sample.PartyCompositionIds.Count
            || sample.CompositionExpectations.Count != sample.PartyCompositionIds.Count)
        {
            return false;
        }

        return sample.PartyCompositionIds.All(id => sample.CompositionExpectations.ContainsKey(id))
               && sample.CompositionExpectations.Keys.All(id =>
                   sample.PartyCompositionIds.Contains(id, StringComparer.OrdinalIgnoreCase));
    }

    private static T Read<T>(string path, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), options)
        ?? throw new InvalidOperationException($"Could not deserialize encounter calibration content '{path}'.");

    private static Dictionary<AttributeType, float> WithRequiredAttributes(
        IReadOnlyDictionary<AttributeType, float> authored)
    {
        var result = new Dictionary<AttributeType, float>
        {
            [AttributeType.MaxHealth] = 1,
            [AttributeType.Power] = 0,
            [AttributeType.CritDamage] = 100,
            [AttributeType.DodgeChance] = 0
        };
        foreach (var (attribute, value) in authored)
            result[attribute] = value;
        return result;
    }

    private static readonly AttributeType[] DungeonScaledAttributes =
    [
        AttributeType.MaxHealth,
        AttributeType.Power,
        AttributeType.Armor,
        AttributeType.Resistance,
        AttributeType.ArmorPenetration,
        AttributeType.MagicPenetration,
        AttributeType.HealthRegeneration
    ];
}

/// <summary>
/// Crosses player/Essence scenarios with matching authored encounters and
/// reports explicit out-of-band balance diagnostics. It never changes live scaling.
/// </summary>
public sealed class EncounterCalibrationRunner
{
    private readonly IAbilityCatalogProvider _catalogProvider;
    private readonly IEssenceDefinitionRepository _essenceDefinitions;

    public EncounterCalibrationRunner(
        IAbilityCatalogProvider catalogProvider,
        IEssenceDefinitionRepository essenceDefinitions)
    {
        _catalogProvider = catalogProvider;
        _essenceDefinitions = essenceDefinitions;
    }

    public EncounterCalibrationReport Run(
        EncounterCalibrationCatalog encounterCatalog,
        IReadOnlyList<EssenceProgressionCalibrationScenario> playerScenarios,
        EncounterCalibrationRunOptions? runOptions = null)
    {
        ArgumentNullException.ThrowIfNull(encounterCatalog);
        ArgumentNullException.ThrowIfNull(playerScenarios);
        runOptions ??= new EncounterCalibrationRunOptions();
        if (runOptions.SampleCount is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(runOptions), "Sample count must be between 1 and 1,000.");
        var catalog = _catalogProvider.GetCatalog();
        var essenceDefinitions = _essenceDefinitions.GetAll()
            .ToDictionary(definition => definition.Id, StringComparer.OrdinalIgnoreCase);
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var compiledSummons = AbilityCompiler.CompileSummons(catalog.Summons);
        var compiledCatalogAbilities = AbilityCompiler.CompileAbilities(catalog.Abilities);
        var results = new List<EncounterCalibrationResult>();

        foreach (var encounter in encounterCatalog.Encounters.Where(encounter =>
                     Includes(runOptions.EncounterIds, encounter.Id)))
        {
            var matchingPlayers = playerScenarios.Where(player =>
                    player.SnapshotAnchorId.Equals(
                        encounter.SnapshotAnchorId,
                        StringComparison.OrdinalIgnoreCase)
                    && Includes(runOptions.GearEnvelopeIds, player.GearEnvelopeId))
                .ToList();
            if (matchingPlayers.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Encounter '{encounter.Id}' has no player scenarios for anchor '{encounter.SnapshotAnchorId}'.");
            }

            if (encounter.PartyCompositions.Count > 0)
            {
                foreach (var gearGroup in matchingPlayers.GroupBy(
                             player => player.GearEnvelopeId,
                             StringComparer.OrdinalIgnoreCase))
                {
                    foreach (var composition in encounter.PartyCompositions.Where(composition =>
                                 Includes(runOptions.PartyCompositionIds, composition.Id)))
                    {
                        var envelopeIds = gearGroup.First().Envelopes
                            .Select(envelope => envelope.Id)
                            .Where(id => Includes(runOptions.EssenceEnvelopeIds, id))
                            .ToList();
                        foreach (var envelopeId in envelopeIds)
                        {
                            var members = CreatePartyMembers(
                                gearGroup.ToList(),
                                composition,
                                encounter.PlayerCount,
                                envelopeId,
                                essenceDefinitions);
                            var first = members[0].Player;
                            var randomSeeds = CreateRandomSeeds(first.RandomSeeds, runOptions.SampleCount);
                            var samples = randomSeeds.Select(seed => RunSample(
                                encounter,
                                members,
                                seed,
                                compiledStatuses,
                                compiledSummons,
                                compiledCatalogAbilities,
                                essenceDefinitions)).ToList();
                            results.Add(Aggregate(
                                encounter,
                                first.CharacterLevel,
                                first.GearEnvelopeId,
                                "mixed",
                                composition.Id,
                                composition.Id,
                                envelopeId,
                                samples));
                        }
                    }
                }

                continue;
            }

            foreach (var player in matchingPlayers.Where(player =>
                         Includes(runOptions.BuildFamilyIds, player.BuildFamilyId)))
            {
                foreach (var envelope in player.Envelopes.Where(envelope =>
                             Includes(runOptions.EssenceEnvelopeIds, envelope.Id)))
                {
                    var member = new EncounterCalibrationPlayerMember(
                        player,
                        envelope,
                        CompileEssenceAbilities(envelope, essenceDefinitions));
                    var randomSeeds = CreateRandomSeeds(player.RandomSeeds, runOptions.SampleCount);
                    var samples = randomSeeds.Select(seed => RunSample(
                        encounter,
                        [member],
                        seed,
                        compiledStatuses,
                        compiledSummons,
                        compiledCatalogAbilities,
                        essenceDefinitions)).ToList();
                    results.Add(Aggregate(
                        encounter,
                        player.CharacterLevel,
                        player.GearEnvelopeId,
                        player.AllocationProfileId,
                        player.BuildFamilyId,
                        string.Empty,
                        envelope.Id,
                        samples));
                }
            }
        }

        if (results.Count == 0)
            throw new InvalidOperationException("The encounter calibration filters did not select any result rows.");

        return new EncounterCalibrationReport(
            results,
            CreateExceptions(encounterCatalog, results));
    }

    private static bool Includes(IReadOnlyCollection<string>? filter, string value) =>
        filter is null
        || filter.Count == 0
        || filter.Contains(value, StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyList<int> CreateRandomSeeds(
        IReadOnlyList<int> configuredSeeds,
        int? requestedSampleCount)
    {
        var sampleCount = requestedSampleCount ?? configuredSeeds.Count;
        var seeds = configuredSeeds.Take(sampleCount).ToList();
        var used = seeds.ToHashSet();
        for (var index = seeds.Count; index < sampleCount; index++)
        {
            var mixed = 0x9E3779B9u * (uint)(index + 1) ^ 0xA5A5A5A5u;
            var candidate = (int)(mixed & 0x7FFF_FFFFu);
            if (candidate == 0)
                candidate = 1;
            while (!used.Add(candidate))
                candidate = candidate == int.MaxValue ? 1 : candidate + 1;
            seeds.Add(candidate);
        }

        return seeds;
    }

    private static IReadOnlyList<CompiledAbility> CompileEssenceAbilities(
        EssenceProgressionCalibrationEnvelope envelope,
        IReadOnlyDictionary<string, Domain.Models.Essences.Definitions.EssenceDefinition> definitions) =>
        envelope.Essences.SelectMany(entry =>
            {
                var definition = definitions[entry.EssenceId];
                return new[] { definition.ActiveAbility, definition.PassiveAbility }
                    .Where(ability => !string.IsNullOrWhiteSpace(ability.Id))
                    .Select(ability => new
                    {
                        Ability = EssenceAbilityProgressionScaler.Apply(ability, entry.AscensionTier),
                        entry.AscensionTier
                    });
            })
            .GroupBy(item => item.Ability.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.AscensionTier).First().Ability)
            .Select(AbilityCompiler.CompileAbility)
            .ToList();

    private static IReadOnlyList<EncounterCalibrationPlayerMember> CreatePartyMembers(
        IReadOnlyList<EssenceProgressionCalibrationScenario> players,
        EncounterCalibrationPartyComposition composition,
        int playerCount,
        string envelopeId,
        IReadOnlyDictionary<string, Domain.Models.Essences.Definitions.EssenceDefinition> definitions)
    {
        var playersByFamily = players.ToDictionary(
            player => player.BuildFamilyId,
            StringComparer.OrdinalIgnoreCase);
        var members = new List<EncounterCalibrationPlayerMember>(playerCount);
        for (var index = 0; index < playerCount; index++)
        {
            var familyId = GetBuildFamilyForRosterSlot(composition, index, playerCount);
            if (!playersByFamily.TryGetValue(familyId, out var player))
            {
                throw new InvalidOperationException(
                    $"Party composition '{composition.Id}' references unavailable build family '{familyId}'.");
            }

            var envelope = player.Envelopes.Single(candidate =>
                candidate.Id.Equals(envelopeId, StringComparison.OrdinalIgnoreCase));
            members.Add(new EncounterCalibrationPlayerMember(
                player,
                envelope,
                CompileEssenceAbilities(envelope, definitions)));
        }

        var seeds = members[0].Player.RandomSeeds;
        if (members.Any(member => !member.Player.RandomSeeds.SequenceEqual(seeds)))
            throw new InvalidOperationException($"Party composition '{composition.Id}' mixes incompatible random seeds.");
        return members;
    }

    private static string GetBuildFamilyForRosterSlot(
        EncounterCalibrationPartyComposition composition,
        int index,
        int playerCount)
    {
        var rosterOverride = composition.RosterOverrides?.SingleOrDefault(roster =>
            roster.PlayerCount == playerCount);
        if (rosterOverride is not null)
            return rosterOverride.MemberBuildFamilyIds[index];

        var patternCount = composition.MemberBuildFamilyIds.Count;
        var completePatternSlots = playerCount / patternCount * patternCount;
        if (index < completePatternSlots)
            return composition.MemberBuildFamilyIds[index % patternCount];

        var remainderCount = playerCount - completePatternSlots;
        var remainderIndex = index - completePatternSlots;
        var patternIndex = Math.Min(
            patternCount - 1,
            (int)Math.Floor((remainderIndex + 0.5d) * patternCount / remainderCount));
        return composition.MemberBuildFamilyIds[patternIndex];
    }

    private static EncounterCalibrationSampleResult RunSample(
        AuthoredEncounterCalibrationScenario encounter,
        IReadOnlyList<EncounterCalibrationPlayerMember> members,
        int randomSeed,
        IReadOnlyDictionary<string, CompiledStatus> compiledStatuses,
        IReadOnlyDictionary<string, CompiledSummon> compiledSummons,
        IReadOnlyDictionary<string, CompiledAbility> compiledCatalogAbilities,
        IReadOnlyDictionary<string, Domain.Models.Essences.Definitions.EssenceDefinition> essenceDefinitions)
    {
        var friendly = members.Select((member, index) => new RuntimeCombatant(
                $"calibration-player-{index + 1}",
                $"{member.Player.BuildFamilyId} Player {index + 1}",
                CombatTeam.Friendly,
                WithRequiredAttributes(member.Player.PlayerAttributes),
                member.Abilities,
                ["Role.Calibration", $"Build.{member.Player.BuildFamilyId}"],
                partyNumber: index / WorldTowerPartyRules.MaximumPartySize + 1))
            .ToList();
        var tagsByMonsterId = essenceDefinitions.Values
            .GroupBy(definition => definition.SourceMonsterId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group.SelectMany(definition => definition.Tags)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
        var hostile = encounter.Hostiles.Select(definition => new RuntimeCombatant(
                definition.Id,
                definition.Name,
                CombatTeam.Hostile,
                new Dictionary<AttributeType, float>(definition.Attributes),
                definition.AbilityIds.Select(id => ApplyCooldownDelay(
                    compiledCatalogAbilities[id],
                    definition.AbilityCooldownDelayFraction)).ToList(),
                tagsByMonsterId.GetValueOrDefault(definition.MonsterId) ?? [],
                staggerDefinition: definition.StaggerDefinition,
                staggerParticipantCount: definition.StaggerParticipantCount))
            .ToList();
        var engine = new FastCombatEngine(
            compiledStatuses,
            compiledSummons,
            compiledCatalogAbilities,
            new FastCombatEngineOptions(
                encounter.MaxTicks,
                RandomSeed: randomSeed,
                CaptureEventLog: true,
                OvertimeStartsAtTick: encounter.OvertimeStartsAtTick,
                OvertimePowerIncreaseIntervalTicks: encounter.OvertimePowerIncreaseIntervalTicks,
                OvertimePowerIncreasePercent: encounter.OvertimePowerIncreasePercent));
        var result = engine.Run(friendly, hostile);
        var friendlyIds = friendly.Select(combatant => combatant.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var friendlyStats = result.EntityStats.Where(stats => friendlyIds.Contains(stats.EntityId)).ToList();
        var hostileIds = hostile.Select(combatant => combatant.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hostileStats = result.EntityStats.Where(stats => hostileIds.Contains(stats.EntityId)).ToList();
        var totalMaxHealth = friendlyStats.Sum(stats => stats.MaxHealth ?? 0);
        var survivalResources = friendlyStats.Sum(stats => (stats.Health ?? 0) + (stats.Barrier ?? 0));
        var totalBarrierGenerated = friendlyStats.Sum(stats => stats.BarrierGenerated);
        var finalBarrier = friendlyStats.Sum(stats => stats.Barrier ?? 0);
        var totalBarrierConsumed = Math.Max(0, totalBarrierGenerated - finalBarrier);
        var durationMinutes = Math.Max(1, result.Duration)
                              / (double)(FastCombatEngine.TicksPerSecond * 60);
        var friendlyAbilityDamage = GetAbilityDamage(friendlyStats);
        var hostileAbilityDamage = GetAbilityDamage(hostileStats);
        var friendlyBasicAttackDamage = GetBasicAttackDamage(friendlyStats);
        var hostileBasicAttackDamage = GetBasicAttackDamage(hostileStats);
        var firstFriendlyDeathTick = result.EventLog
            .Where(item => item.EventType == EventType.Death && friendlyIds.Contains(item.TargetId))
            .Select(item => (int?)item.Timestamp)
            .Min();
        var totalEnemyMaxHealth = hostileStats.Sum(stats => stats.MaxHealth ?? 0);
        var enemyHealthRemaining = hostileStats.Sum(stats => stats.Health ?? 0);
        var timeout = result.Outcome == BattleOutcome.Draw;
        var staggerBossIds = encounter.Hostiles
            .Where(definition => definition.StaggerDefinition is { Enabled: true })
            .Select(definition => definition.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var stagger = CalculateStaggerTelemetry(result.EventLog, staggerBossIds, result.Duration);
        var totalBossDamage = result.EventLog
            .Where(item => staggerBossIds.Contains(item.TargetId) && IsDamageEvent(item.EventType))
            .Sum(item => Math.Max(0, item.Magnitude));
        var breakCapReached = encounter.Hostiles
            .Where(definition => definition.StaggerDefinition?.MaximumBreaks is > 0)
            .Any(definition => stagger.BreaksByTarget.GetValueOrDefault(definition.Id)
                               >= definition.StaggerDefinition!.MaximumBreaks!.Value);

        return new EncounterCalibrationSampleResult(
            result.Outcome == BattleOutcome.Victory,
            timeout,
            result.Duration,
            friendlyStats.Sum(stats => stats.DamageTaken),
            totalMaxHealth > 0 ? 100d * survivalResources / totalMaxHealth : 0,
            friendlyStats.Sum(stats => stats.HealingDone),
            friendlyStats.Sum(stats => stats.BarrierGenerated),
            friendlyStats.Sum(stats => stats.HealthRegenerated),
            totalBarrierConsumed,
            friendlyStats.Sum(stats => stats.Abilities.Sum(ability => ability.Uses)) / durationMinutes,
            friendlyStats.Sum(stats => stats.Abilities.Sum(ability => ability.Summons)),
            friendlyStats.Sum(stats => stats.Abilities.Sum(ability => ability.Stuns)),
            hostileStats.Sum(stats => stats.Abilities.Sum(ability => ability.Uses)),
            friendlyStats.Any(stats => (stats.Health ?? 0) <= 0),
            firstFriendlyDeathTick,
            timeout && totalEnemyMaxHealth > 0 ? 100d * enemyHealthRemaining / totalEnemyMaxHealth : null,
            friendlyBasicAttackDamage,
            friendlyAbilityDamage.Values.Sum(),
            hostileBasicAttackDamage,
            hostileAbilityDamage.Values.Sum(),
            friendlyStats.Sum(stats => stats.HealthRegenerationOverhealed),
            totalBarrierGenerated > 0 ? 100d * finalBarrier / totalBarrierGenerated : 0,
            friendlyStats.Sum(stats => stats.Abilities.Sum(ability => ability.Stuns)) / durationMinutes,
            friendlyAbilityDamage,
            hostileAbilityDamage,
            staggerBossIds.Count > 0,
            friendlyStats.Sum(stats => stats.StaggerContributed),
            stagger.BreakCount,
            stagger.FirstBreakTick,
            result.Duration > 0 ? 100d * stagger.StaggeredTicks / result.Duration : 0,
            totalBossDamage > 0 ? 100d * stagger.DamageDuringStagger / totalBossDamage : 0,
            breakCapReached);
    }

    private static CompiledAbility ApplyCooldownDelay(
        CompiledAbility ability,
        double delayFraction)
    {
        if (delayFraction <= 0)
            return ability;

        return new CompiledAbility
        {
            Id = ability.Id,
            Name = ability.Name,
            Kind = ability.Kind,
            CooldownTicks = ScaleCooldown(ability.CooldownTicks, delayFraction),
            ThreatValue = ability.ThreatValue,
            ThreatMultiplier = ability.ThreatMultiplier,
            Costs = ability.Costs,
            Tags = ability.Tags,
            TriggersByEvent = ability.TriggersByEvent.ToDictionary(
                entry => entry.Key,
                entry => (IReadOnlyList<CompiledTrigger>)entry.Value.Select(trigger => new CompiledTrigger
                {
                    Event = trigger.Event,
                    ThreatValue = trigger.ThreatValue,
                    ThreatInternalCooldownTicks = trigger.ThreatInternalCooldownTicks,
                    InternalCooldownTicks = ScaleCooldown(trigger.InternalCooldownTicks, delayFraction),
                    InitialDelayTicks = trigger.InitialDelayTicks,
                    EveryNthOccurrence = trigger.EveryNthOccurrence,
                    Conditions = trigger.Conditions,
                    Effects = trigger.Effects
                }).ToList())
        };
    }

    private static int ScaleCooldown(int ticks, double delayFraction) =>
        ticks <= 0 ? ticks : Math.Max(1, (int)Math.Ceiling(ticks * (1d + delayFraction)));

    private static StaggerTelemetry CalculateStaggerTelemetry(
        IReadOnlyList<CombatLogItem> eventLog,
        IReadOnlySet<string> staggerBossIds,
        int durationTicks)
    {
        var activeSince = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var breaksByTarget = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var staggeredTicks = 0;
        var damageDuringStagger = 0;
        int? firstBreakTick = null;
        foreach (var item in eventLog.OrderBy(item => item.Timestamp))
        {
            if (!staggerBossIds.Contains(item.TargetId))
                continue;

            if (item.EventType == EventType.StaggerBroken)
            {
                breaksByTarget[item.TargetId] = breaksByTarget.GetValueOrDefault(item.TargetId) + 1;
                activeSince.TryAdd(item.TargetId, item.Timestamp);
                firstBreakTick = !firstBreakTick.HasValue
                    ? item.Timestamp
                    : Math.Min(firstBreakTick.Value, item.Timestamp);
            }
            else if (item.EventType == EventType.StaggerRecovered
                     && activeSince.Remove(item.TargetId, out var startedAt))
            {
                staggeredTicks += Math.Max(0, item.Timestamp - startedAt);
            }
            else if (activeSince.ContainsKey(item.TargetId) && IsDamageEvent(item.EventType))
            {
                damageDuringStagger += Math.Max(0, item.Magnitude);
            }
        }

        foreach (var startedAt in activeSince.Values)
            staggeredTicks += Math.Max(0, durationTicks - startedAt);

        return new StaggerTelemetry(
            breaksByTarget.Values.Sum(),
            firstBreakTick,
            staggeredTicks,
            damageDuringStagger,
            breaksByTarget);
    }

    private static bool IsDamageEvent(EventType eventType) =>
        eventType is EventType.Damage
            or EventType.DamageOverTime
            or EventType.DamageCrit
            or EventType.ReflectedDamage;

    private static EncounterCalibrationResult Aggregate(
        AuthoredEncounterCalibrationScenario encounter,
        int characterLevel,
        string gearEnvelopeId,
        string allocationProfileId,
        string buildFamilyId,
        string partyCompositionId,
        string essenceEnvelopeId,
        IReadOnlyList<EncounterCalibrationSampleResult> samples)
    {
        var topFriendlyAbility = GetTopAbility(samples.Select(sample => sample.FriendlyAbilityDamage));
        var topEnemyAbility = GetTopAbility(samples.Select(sample => sample.EnemyAbilityDamage));
        var firstDeathTicks = samples.Where(sample => sample.FirstFriendlyDeathTick.HasValue)
            .Select(sample => sample.FirstFriendlyDeathTick!.Value)
            .ToList();
        var timeoutEnemyHealth = samples.Where(sample => sample.EnemyHealthRemainingOnTimeoutPercent.HasValue)
            .Select(sample => sample.EnemyHealthRemainingOnTimeoutPercent!.Value)
            .ToList();
        var firstStaggerBreakTicks = samples.Where(sample => sample.FirstStaggerBreakTick.HasValue)
            .Select(sample => sample.FirstStaggerBreakTick!.Value)
            .ToList();
        var victories = samples.Count(sample => sample.Victory);
        var timeouts = samples.Count(sample => sample.Timeout);
        var winConfidence = CalculateWilsonConfidenceInterval(victories, samples.Count);
        var timeoutConfidence = CalculateWilsonConfidenceInterval(timeouts, samples.Count);

        return new EncounterCalibrationResult(
            encounter.Id,
            encounter.ContentType,
            encounter.DifficultyRole,
            encounter.SnapshotAnchorId,
            characterLevel,
            gearEnvelopeId,
            allocationProfileId,
            buildFamilyId,
            partyCompositionId,
            essenceEnvelopeId,
            encounter.PlayerCount,
            encounter.Hostiles.Count,
            samples.Count,
            victories / (double)samples.Count,
            winConfidence.Lower,
            winConfidence.Upper,
            timeouts / (double)samples.Count,
            timeoutConfidence.Lower,
            timeoutConfidence.Upper,
            samples.Average(sample => sample.DurationTicks),
            samples.Average(sample => sample.DamageTaken),
            samples.Average(sample => sample.SurvivalResourcePercent),
            samples.Average(sample => sample.HealingDone),
            samples.Average(sample => sample.BarrierGenerated),
            samples.Average(sample => sample.AbilityUsesPerMinute),
            samples.Average(sample => sample.Summons),
            samples.Average(sample => sample.Stuns),
            samples.Average(sample => sample.EnemyAbilityUses),
            samples.Count(sample => sample.FriendlyDeath) / (double)samples.Count,
            firstDeathTicks.Count > 0 ? firstDeathTicks.Average() : null,
            timeoutEnemyHealth.Count > 0 ? timeoutEnemyHealth.Average() : null,
            samples.Average(sample => sample.FriendlyBasicAttackDamage),
            samples.Average(sample => sample.FriendlyAbilityDamageTotal),
            samples.Average(sample => sample.EnemyBasicAttackDamage),
            samples.Average(sample => sample.EnemyAbilityDamageTotal),
            samples.Average(sample => sample.HealthRegenerationOverhealed),
            samples.Average(sample => sample.UnusedBarrierPercent),
            samples.Average(sample => sample.StunsPerMinute),
            topFriendlyAbility.Name,
            topFriendlyAbility.TotalDamage / (double)samples.Count,
            topEnemyAbility.Name,
            topEnemyAbility.TotalDamage / (double)samples.Count,
            samples.Any(sample => sample.StaggerEnabled),
            samples.Average(sample => sample.StaggerContributed),
            samples.Average(sample => sample.StaggerBreaks),
            firstStaggerBreakTicks.Count > 0 ? firstStaggerBreakTicks.Average() : null,
            samples.Average(sample => sample.StaggerUptimePercent),
            samples.Average(sample => sample.DamageDuringStaggerPercent),
            samples.Count(sample => sample.StaggerBreakCapReached) / (double)samples.Count,
            IsIncludedInRoleAssessment(encounter, buildFamilyId, partyCompositionId),
            CountSustainMembers(encounter, buildFamilyId, partyCompositionId),
            samples.Average(sample => sample.HealthRegenerated),
            samples.Average(sample => sample.BarrierConsumed),
            GetCompositionExpectation(encounter, partyCompositionId));
    }

    private static int CountSustainMembers(
        AuthoredEncounterCalibrationScenario encounter,
        string buildFamilyId,
        string partyCompositionId)
    {
        if (string.IsNullOrWhiteSpace(partyCompositionId))
        {
            return buildFamilyId.Equals("sustain", StringComparison.OrdinalIgnoreCase)
                ? 1
                : 0;
        }

        var composition = encounter.PartyCompositions.Single(candidate =>
            candidate.Id.Equals(partyCompositionId, StringComparison.OrdinalIgnoreCase));
        return Enumerable.Range(0, encounter.PlayerCount)
            .Count(index => GetBuildFamilyForRosterSlot(composition, index, encounter.PlayerCount)
                .Equals("sustain", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsIncludedInRoleAssessment(
        AuthoredEncounterCalibrationScenario encounter,
        string buildFamilyId,
        string partyCompositionId)
    {
        if (!string.IsNullOrWhiteSpace(partyCompositionId))
        {
            return GetCompositionExpectation(encounter, partyCompositionId)
                   != CompositionExpectation.Observational;
        }

        var assessedFamilies = encounter.Band.AssessedBuildFamilyIds;
        return assessedFamilies is null
               || assessedFamilies.Count == 0
               || assessedFamilies.Contains(buildFamilyId, StringComparer.OrdinalIgnoreCase);
    }

    private static CompositionExpectation GetCompositionExpectation(
        AuthoredEncounterCalibrationScenario encounter,
        string partyCompositionId) =>
        string.IsNullOrWhiteSpace(partyCompositionId)
            ? CompositionExpectation.NotApplicable
            : encounter.CompositionExpectations[partyCompositionId];

    private static ConfidenceInterval CalculateWilsonConfidenceInterval(int successes, int samples)
    {
        const double z = 1.959963984540054;
        var rate = successes / (double)samples;
        var zSquared = z * z;
        var denominator = 1 + zSquared / samples;
        var center = (rate + zSquared / (2 * samples)) / denominator;
        var margin = z * Math.Sqrt(
            rate * (1 - rate) / samples + zSquared / (4d * samples * samples)) / denominator;
        return new ConfidenceInterval(
            ClampProbability(center - margin),
            ClampProbability(center + margin));
    }

    private static double ClampProbability(double value)
    {
        if (value < 0.000_000_000_001)
            return 0;
        if (value > 0.999_999_999_999)
            return 1;
        return value;
    }

    private static IReadOnlyDictionary<string, int> GetAbilityDamage(
        IReadOnlyList<EntityStats> stats) =>
        stats.SelectMany(entity => entity.Abilities)
            .Where(ability => ability.TotalDamage > 0
                              && !ability.Name.Equals("Basic Attack", StringComparison.OrdinalIgnoreCase))
            .GroupBy(ability => ability.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(ability => ability.TotalDamage),
                StringComparer.OrdinalIgnoreCase);

    private static int GetBasicAttackDamage(IReadOnlyList<EntityStats> stats) =>
        stats.SelectMany(entity => entity.Abilities)
            .Where(ability => ability.Name.Equals("Basic Attack", StringComparison.OrdinalIgnoreCase))
            .Sum(ability => ability.TotalDamage);

    private static AbilityDamageTotal GetTopAbility(
        IEnumerable<IReadOnlyDictionary<string, int>> samples)
    {
        var top = samples.SelectMany(sample => sample)
            .GroupBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new AbilityDamageTotal(
                group.Key,
                group.Sum(entry => entry.Value)))
            .OrderByDescending(entry => entry.TotalDamage)
            .ThenBy(entry => entry.Name, StringComparer.Ordinal)
            .FirstOrDefault();
        return top ?? new AbilityDamageTotal(string.Empty, 0);
    }

    private static IReadOnlyList<EncounterCalibrationException> CreateExceptions(
        EncounterCalibrationCatalog catalog,
        IReadOnlyList<EncounterCalibrationResult> results)
    {
        var exceptions = new List<EncounterCalibrationException>();
        var assessed = results.Where(result =>
                result.GearEnvelopeId.Equals(catalog.AssessmentGearEnvelopeId, StringComparison.OrdinalIgnoreCase)
                && result.EssenceEnvelopeId.Equals(catalog.AssessmentEssenceEnvelopeId, StringComparison.OrdinalIgnoreCase)
                && result.IncludedInRoleAssessment)
            .ToList();
        var encounters = catalog.Encounters.ToDictionary(encounter => encounter.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var result in assessed)
        {
            var band = encounters[result.EncounterId].Band;
            switch (result.CompositionExpectation)
            {
                case CompositionExpectation.Alternative:
                    AddMinimumExpectation(
                        exceptions,
                        result,
                        "WinRate",
                        result.WinRate,
                        catalog.CompositionAssessment.AlternativeMinimumWinRate,
                        "UnexpectedFailure");
                    AddMaximumExpectation(
                        exceptions,
                        result,
                        "TimeoutRate",
                        result.TimeoutRate,
                        catalog.CompositionAssessment.AlternativeMaximumTimeoutRate,
                        "UnexpectedFailure");
                    continue;
                case CompositionExpectation.Countered:
                    AddMaximumExpectation(
                        exceptions,
                        result,
                        "WinRate",
                        result.WinRate,
                        catalog.CompositionAssessment.CounteredMaximumWinRate,
                        "UnexpectedSuccess");
                    continue;
                case CompositionExpectation.Challenge:
                    AddMaximumExpectation(
                        exceptions,
                        result,
                        "WinRate",
                        result.WinRate,
                        catalog.CompositionAssessment.ChallengeMaximumWinRate,
                        "UnexpectedSuccess");
                    continue;
                case CompositionExpectation.Observational:
                    continue;
            }

            AddExpectedBandExceptions(exceptions, result, band);
            if (result.StaggerEnabled && band.Stagger is not null)
            {
                AddOutsideBand(
                    exceptions,
                    result,
                    "AverageStaggerBreaks",
                    result.AverageStaggerBreaks,
                    band.Stagger.MinimumAverageBreaks,
                    band.Stagger.MaximumAverageBreaks);
                AddOutsideBand(
                    exceptions,
                    result,
                    "AverageFirstStaggerBreakTick",
                    result.AverageFirstStaggerBreakTick
                    ?? band.Stagger.MaximumAverageFirstBreakTick + 1d,
                    0,
                    band.Stagger.MaximumAverageFirstBreakTick);
                AddOutsideBand(
                    exceptions,
                    result,
                    "AverageStaggerUptimePercent",
                    result.AverageStaggerUptimePercent,
                    0,
                    band.Stagger.MaximumUptimePercent);
                AddOutsideBand(
                    exceptions,
                    result,
                    "StaggerBreakCapRate",
                    result.StaggerBreakCapRate,
                    0,
                    band.Stagger.MaximumBreakCapRate);
            }
        }

        foreach (var group in assessed
                     .Where(result => string.IsNullOrWhiteSpace(result.PartyCompositionId)
                                      || result.CompositionExpectation == CompositionExpectation.Expected)
                     .GroupBy(result => result.EncounterId, StringComparer.OrdinalIgnoreCase))
        {
            var spread = group.Max(result => result.WinRate) - group.Min(result => result.WinRate);
            if (spread > catalog.MaximumBuildWinRateSpread)
            {
                exceptions.Add(new EncounterCalibrationException(
                    group.Key,
                    "all",
                    "BuildSensitive",
                    "BuildWinRateSpread",
                    spread,
                    0,
                    catalog.MaximumBuildWinRateSpread));
            }
        }

        return exceptions;
    }

    private static void AddExpectedBandExceptions(
        ICollection<EncounterCalibrationException> exceptions,
        EncounterCalibrationResult result,
        EncounterCalibrationBand band)
    {
        AddOutsideBand(exceptions, result, "WinRate", result.WinRate, band.MinimumWinRate, band.MaximumWinRate);
        if (result.TimeoutRate > band.MaximumTimeoutRate)
        {
            exceptions.Add(new EncounterCalibrationException(
                result.EncounterId,
                result.BuildFamilyId,
                "Timeout",
                "TimeoutRate",
                result.TimeoutRate,
                0,
                band.MaximumTimeoutRate));
        }
        AddOutsideBand(
            exceptions,
            result,
            "AverageDurationTicks",
            result.AverageDurationTicks,
            band.MinimumDurationTicks,
            band.MaximumDurationTicks);
        if (result.AverageSurvivalResourcePercent < band.MinimumSurvivalResourcePercent)
        {
            exceptions.Add(new EncounterCalibrationException(
                result.EncounterId,
                result.BuildFamilyId,
                "LowSurvival",
                "AverageSurvivalResourcePercent",
                result.AverageSurvivalResourcePercent,
                band.MinimumSurvivalResourcePercent,
                100));
        }
    }

    private static void AddMinimumExpectation(
        ICollection<EncounterCalibrationException> exceptions,
        EncounterCalibrationResult result,
        string metric,
        double actual,
        double minimum,
        string classification)
    {
        if (actual >= minimum)
            return;
        exceptions.Add(new EncounterCalibrationException(
            result.EncounterId,
            result.BuildFamilyId,
            classification,
            metric,
            actual,
            minimum,
            1));
    }

    private static void AddMaximumExpectation(
        ICollection<EncounterCalibrationException> exceptions,
        EncounterCalibrationResult result,
        string metric,
        double actual,
        double maximum,
        string classification)
    {
        if (actual <= maximum)
            return;
        exceptions.Add(new EncounterCalibrationException(
            result.EncounterId,
            result.BuildFamilyId,
            classification,
            metric,
            actual,
            0,
            maximum));
    }

    private static void AddOutsideBand(
        ICollection<EncounterCalibrationException> exceptions,
        EncounterCalibrationResult result,
        string metric,
        double actual,
        double minimum,
        double maximum)
    {
        if (actual >= minimum && actual <= maximum)
            return;
        exceptions.Add(new EncounterCalibrationException(
            result.EncounterId,
            result.BuildFamilyId,
            actual < minimum ? "BelowBand" : "AboveBand",
            metric,
            actual,
            minimum,
            maximum));
    }

    private static Dictionary<AttributeType, float> WithRequiredAttributes(
        IReadOnlyDictionary<AttributeType, float> authored)
    {
        var result = new Dictionary<AttributeType, float>
        {
            [AttributeType.MaxHealth] = 1,
            [AttributeType.Power] = 0,
            [AttributeType.CritDamage] = 100,
            [AttributeType.DodgeChance] = 0
        };
        foreach (var (attribute, value) in authored)
            result[attribute] = value;
        return result;
    }

    private sealed record EncounterCalibrationSampleResult(
        bool Victory,
        bool Timeout,
        int DurationTicks,
        int DamageTaken,
        double SurvivalResourcePercent,
        int HealingDone,
        int BarrierGenerated,
        int HealthRegenerated,
        int BarrierConsumed,
        double AbilityUsesPerMinute,
        int Summons,
        int Stuns,
        int EnemyAbilityUses,
        bool FriendlyDeath,
        int? FirstFriendlyDeathTick,
        double? EnemyHealthRemainingOnTimeoutPercent,
        int FriendlyBasicAttackDamage,
        int FriendlyAbilityDamageTotal,
        int EnemyBasicAttackDamage,
        int EnemyAbilityDamageTotal,
        int HealthRegenerationOverhealed,
        double UnusedBarrierPercent,
        double StunsPerMinute,
        IReadOnlyDictionary<string, int> FriendlyAbilityDamage,
        IReadOnlyDictionary<string, int> EnemyAbilityDamage,
        bool StaggerEnabled,
        int StaggerContributed,
        int StaggerBreaks,
        int? FirstStaggerBreakTick,
        double StaggerUptimePercent,
        double DamageDuringStaggerPercent,
        bool StaggerBreakCapReached);

    private sealed record EncounterCalibrationPlayerMember(
        EssenceProgressionCalibrationScenario Player,
        EssenceProgressionCalibrationEnvelope Envelope,
        IReadOnlyList<CompiledAbility> Abilities);

    private sealed record AbilityDamageTotal(string Name, int TotalDamage);

    private sealed record StaggerTelemetry(
        int BreakCount,
        int? FirstBreakTick,
        int StaggeredTicks,
        int DamageDuringStagger,
        IReadOnlyDictionary<string, int> BreaksByTarget);

    private sealed record ConfidenceInterval(double Lower, double Upper);
}

public sealed class EncounterCalibrationMatrixRunner
{
    private readonly AuthoredEncounterCalibrationFactory _encounters;
    private readonly EssenceCalibrationMatrixFactory _players;
    private readonly EncounterCalibrationRunner _runner;

    public EncounterCalibrationMatrixRunner(
        AuthoredEncounterCalibrationFactory encounters,
        EssenceCalibrationMatrixFactory players,
        EncounterCalibrationRunner runner)
    {
        _encounters = encounters;
        _players = players;
        _runner = runner;
    }

    public EncounterCalibrationReport Run() =>
        _runner.Run(_encounters.CreateCatalog(), _players.CreateScenarios());
}

public enum EncounterCalibrationContentType
{
    Idle,
    Dungeon,
    Tower,
    Raid
}

public enum CompositionExpectation
{
    NotApplicable,
    Expected,
    Alternative,
    Countered,
    Challenge,
    Observational
}

public sealed record EncounterCalibrationCatalog(
    int Version,
    string AssessmentGearEnvelopeId,
    string AssessmentEssenceEnvelopeId,
    double MaximumBuildWinRateSpread,
    EncounterCalibrationSupportAssessment SupportAssessment,
    EncounterCalibrationCompositionAssessment CompositionAssessment,
    IReadOnlyList<EncounterCalibrationPartyComposition> PartyCompositions,
    IReadOnlyList<AuthoredEncounterCalibrationScenario> Encounters);

public sealed record EncounterCalibrationSupportAssessment(
    string BaselineCompositionId = "balanced",
    string SupportCompositionId = "sustain-heavy",
    double MinimumDeathRateReduction = 0.10,
    double MinimumSurvivalResourceIncreasePercent = 5,
    int MinimumFirstDeathDelayTicks = 60,
    double MaximumDurationIncreaseRate = 0.25);

public sealed record EncounterCalibrationCompositionAssessment(
    double AlternativeMinimumWinRate = 0.20,
    double AlternativeMaximumTimeoutRate = 0.60,
    double CounteredMaximumWinRate = 0.35,
    double ChallengeMaximumWinRate = 0.65);

public sealed record AuthoredEncounterCalibrationScenario(
    string Id,
    EncounterCalibrationContentType ContentType,
    string DifficultyRole,
    string SnapshotAnchorId,
    int ProgressionPosition,
    int PlayerCount,
    int MaxTicks,
    EncounterCalibrationBand Band,
    IReadOnlyList<EncounterCalibrationHostile> Hostiles,
    IReadOnlyList<EncounterCalibrationPartyComposition> PartyCompositions,
    IReadOnlyDictionary<string, CompositionExpectation> CompositionExpectations,
    int OvertimeStartsAtTick = int.MaxValue,
    int OvertimePowerIncreaseIntervalTicks = 300,
    float OvertimePowerIncreasePercent = 0);

public sealed record EncounterCalibrationPartyComposition(
    string Id,
    IReadOnlyList<string> MemberBuildFamilyIds,
    IReadOnlyList<EncounterCalibrationRosterOverride>? RosterOverrides = null);

public sealed record EncounterCalibrationRosterOverride(
    int PlayerCount,
    IReadOnlyList<string> MemberBuildFamilyIds);

public sealed record EncounterCalibrationHostile(
    string Id,
    string MonsterId,
    string Name,
    IReadOnlyDictionary<AttributeType, float> Attributes,
    IReadOnlyList<string> AbilityIds,
    BossStaggerDefinition? StaggerDefinition = null,
    int StaggerParticipantCount = 1,
    double AbilityCooldownDelayFraction = 0d);

public sealed record EncounterCalibrationBand(
    string Id,
    double MinimumWinRate,
    double MaximumWinRate,
    double MaximumTimeoutRate,
    int MinimumDurationTicks,
    int MaximumDurationTicks,
    double MinimumSurvivalResourcePercent,
    EncounterCalibrationStaggerBand? Stagger = null,
    IReadOnlyList<string>? AssessedBuildFamilyIds = null);

public sealed record EncounterCalibrationStaggerBand(
    double MinimumAverageBreaks,
    double MaximumAverageBreaks,
    int MaximumAverageFirstBreakTick,
    double MaximumUptimePercent,
    double MaximumBreakCapRate);

public sealed record EncounterCalibrationReport(
    IReadOnlyList<EncounterCalibrationResult> Results,
    IReadOnlyList<EncounterCalibrationException> Exceptions);

public sealed record EncounterCalibrationRunOptions(
    IReadOnlyCollection<string>? EncounterIds = null,
    IReadOnlyCollection<string>? GearEnvelopeIds = null,
    IReadOnlyCollection<string>? BuildFamilyIds = null,
    IReadOnlyCollection<string>? PartyCompositionIds = null,
    IReadOnlyCollection<string>? EssenceEnvelopeIds = null,
    int? SampleCount = null);

public sealed record EncounterCalibrationResult(
    string EncounterId,
    EncounterCalibrationContentType ContentType,
    string DifficultyRole,
    string SnapshotAnchorId,
    int CharacterLevel,
    string GearEnvelopeId,
    string AllocationProfileId,
    string BuildFamilyId,
    string PartyCompositionId,
    string EssenceEnvelopeId,
    int PlayerCount,
    int HostileCount,
    int SampleCount,
    double WinRate,
    double WinRateConfidenceLower95,
    double WinRateConfidenceUpper95,
    double TimeoutRate,
    double TimeoutRateConfidenceLower95,
    double TimeoutRateConfidenceUpper95,
    double AverageDurationTicks,
    double AverageDamageTaken,
    double AverageSurvivalResourcePercent,
    double AverageHealingDone,
    double AverageBarrierGenerated,
    double AverageAbilityUsesPerMinute,
    double AverageSummons,
    double AverageStuns,
    double AverageEnemyAbilityUses,
    double FriendlyDeathRate,
    double? AverageFirstFriendlyDeathTick,
    double? AverageEnemyHealthRemainingOnTimeoutPercent,
    double AverageFriendlyBasicAttackDamage,
    double AverageFriendlyAbilityDamage,
    double AverageEnemyBasicAttackDamage,
    double AverageEnemyAbilityDamage,
    double AverageHealthRegenerationOverhealed,
    double AverageUnusedBarrierPercent,
    double AverageStunsPerMinute,
    string TopFriendlyAbilityName,
    double AverageTopFriendlyAbilityDamage,
    string TopEnemyAbilityName,
    double AverageTopEnemyAbilityDamage,
    bool StaggerEnabled = false,
    double AverageStaggerContributed = 0,
    double AverageStaggerBreaks = 0,
    double? AverageFirstStaggerBreakTick = null,
    double AverageStaggerUptimePercent = 0,
    double AverageDamageDuringStaggerPercent = 0,
    double StaggerBreakCapRate = 0,
    bool IncludedInRoleAssessment = true,
    int SustainMemberCount = 0,
    double AverageHealthRegenerated = 0,
    double AverageBarrierConsumed = 0,
    CompositionExpectation CompositionExpectation = CompositionExpectation.NotApplicable);

public sealed record EncounterCalibrationException(
    string EncounterId,
    string BuildFamilyId,
    string Classification,
    string Metric,
    double Actual,
    double Minimum,
    double Maximum);

internal sealed class EncounterCalibrationManifest
{
    public int Version { get; set; }
    public string AssessmentGearEnvelopeId { get; set; } = string.Empty;
    public string AssessmentEssenceEnvelopeId { get; set; } = string.Empty;
    public double MaximumBuildWinRateSpread { get; set; }
    public EncounterCalibrationSupportAssessment SupportAssessment { get; set; } = new();
    public EncounterCalibrationCompositionAssessment CompositionAssessment { get; set; } = new();
    public List<EncounterCalibrationPartyComposition> PartyCompositions { get; set; } = [];
    public List<EncounterCalibrationBand> Bands { get; set; } = [];
    public List<EncounterCalibrationSample> Samples { get; set; } = [];
}

internal sealed class EncounterCalibrationSample
{
    public string Id { get; set; } = string.Empty;
    public EncounterCalibrationContentType ContentType { get; set; }
    public string DifficultyRole { get; set; } = string.Empty;
    public string BandId { get; set; } = string.Empty;
    public string SnapshotAnchorId { get; set; } = string.Empty;
    public string AreaId { get; set; } = string.Empty;
    public List<Guid> CreatureIds { get; set; } = [];
    public string DungeonFamilyId { get; set; } = string.Empty;
    public int DungeonDifficulty { get; set; }
    public string RoomTemplateId { get; set; } = string.Empty;
    public int TowerFloorNumber { get; set; }
    public string RaidBossId { get; set; } = string.Empty;
    public int RaidTier { get; set; }
    public int? RaidPlusLevel { get; set; }
    public int RaidPreparationPercent { get; set; } = 100;
    public int ProgressionPosition { get; set; }
    public List<string> PartyCompositionIds { get; set; } = [];
    public Dictionary<string, CompositionExpectation> CompositionExpectations { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);
    public int MaxTicks { get; set; }
}

internal sealed class CreatureContentDocument
{
    public List<CreatureContentDefinition> Creatures { get; set; } = [];
}

internal sealed class CreatureContentDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public CreatureArchetype Archetype { get; set; } = CreatureArchetype.Balanced;
    public DamageProfile DamageProfile { get; set; } = DamageProfile.Hybrid;
    public DefenseProfile DefenseProfile { get; set; } = DefenseProfile.Balanced;
    public int BaseLevel { get; set; } = 1;
    public int Tier { get; set; } = 1;
    public List<StatOverride> StatOverrides { get; set; } = [];
}

internal sealed class RegionContentDocument
{
    public List<RegionContentDefinition> Regions { get; set; } = [];
}

internal sealed class RegionContentDefinition
{
    public List<AreaContentDefinition> Areas { get; set; } = [];
}

internal sealed class AreaContentDefinition
{
    public string Id { get; set; } = string.Empty;
    public int DifficultyTier { get; set; }
    public List<AreaCreatureContentDefinition> Creatures { get; set; } = [];
}

internal sealed class AreaCreatureContentDefinition
{
    public Guid CreatureId { get; set; }
}

internal sealed class DungeonContentDocument
{
    public List<DungeonFamilyContentDefinition> Families { get; set; } = [];
}

internal sealed class DungeonFamilyContentDefinition
{
    public string Id { get; set; } = string.Empty;
    public int Region { get; set; } = 1;
    public List<DungeonRoomTemplateContentDefinition> RoomTemplates { get; set; } = [];
    public List<DungeonDifficultyContentDefinition> Difficulties { get; set; } = [];
}

internal sealed class DungeonRoomTemplateContentDefinition
{
    public string Id { get; set; } = string.Empty;
    public List<string> EncounterIds { get; set; } = [];
}

internal sealed class DungeonDifficultyContentDefinition
{
    public int Difficulty { get; set; }
    public float EnemyStrengthMultiplier { get; set; } = 1;
}
