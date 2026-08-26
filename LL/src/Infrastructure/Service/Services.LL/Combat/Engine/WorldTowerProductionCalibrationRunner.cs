using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.WorldTower;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Professions.Crafting.V2;
using Domain.Models.Snapshots;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.WorldTower;
using Services.LL.PowerRatings;

namespace Services.LL.Combat.Engine;

public enum WorldTowerCalibrationCohort
{
    BelowRecommended,
    Recommended,
    Stronger
}

public sealed record WorldTowerProductionCalibrationOptions(
    int MinimumFloor = 1,
    int MaximumFloor = 15,
    int SampleCount = 10);

public sealed record WorldTowerEquipmentRequirement(
    int FloorNumber,
    int Tier,
    Rarity Rarity,
    ItemQuality Quality,
    int EssenceCount);

public static class WorldTowerEquipmentRequirementCurve
{
    public const int FirstFloor = 11;
    public const int FinalFloor = 20;

    public static WorldTowerEquipmentRequirement Get(int floorNumber) => floorNumber switch
    {
        11 => new(floorNumber, 2, Rarity.Epic, ItemQuality.Fine, 7),
        12 => new(floorNumber, 2, Rarity.Unique, ItemQuality.Fine, 7),
        13 => new(floorNumber, 2, Rarity.Unique, ItemQuality.Fine, 8),
        14 => new(floorNumber, 2, Rarity.Legendary, ItemQuality.Fine, 8),
        15 => new(floorNumber, 2, Rarity.Legendary, ItemQuality.Fine, 9),
        16 => new(floorNumber, 2, Rarity.Unique, ItemQuality.Exceptional, 9),
        17 => new(floorNumber, 2, Rarity.Legendary, ItemQuality.Fine, 10),
        18 => new(floorNumber, 2, Rarity.Epic, ItemQuality.Exceptional, 10),
        19 => new(floorNumber, 2, Rarity.Unique, ItemQuality.Exceptional, 10),
        20 => new(floorNumber, 2, Rarity.Legendary, ItemQuality.Exceptional, 10),
        _ => throw new ArgumentOutOfRangeException(
            nameof(floorNumber),
            floorNumber,
            $"Late World Tower equipment requirements cover Floors {FirstFloor}–{FinalFloor}.")
    };
}

public sealed record WorldTowerPreparedEquipmentModifier(
    AttributeType Attribute,
    float Amount,
    ModifierType ModifierType);

public sealed record WorldTowerPreparedEquipment(
    string ItemBaseId,
    EquipmentType Type,
    int Tier,
    Rarity Rarity,
    ItemQuality Quality,
    string? RecipeId,
    string? BlueprintId,
    string? EquipmentSetId,
    IReadOnlyList<WorldTowerPreparedEquipmentModifier> Modifiers);

public sealed record WorldTowerPreparedCombatant(
    string Id,
    string Name,
    int Level,
    int? PartyNumber,
    int PowerRating,
    IReadOnlyDictionary<AttributeType, int> FinalAttributes,
    IReadOnlyList<WorldTowerPreparedEquipment> Equipment,
    IReadOnlyList<string> EquipmentSetIds,
    IReadOnlyList<string> EssenceIds,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> AbilityIds);

public sealed record WorldTowerProductionCalibrationResult(
    int FloorNumber,
    WorldTowerCalibrationCohort Cohort,
    string EquipmentRungId,
    int EssenceCount,
    int RosterSize,
    double AveragePowerRating,
    int SampleCount,
    double WinRate,
    double TimeoutRate,
    double AverageDurationTicks,
    bool AbilitiesStartOnCooldown,
    IReadOnlyList<WorldTowerPreparedCombatant> PreparedRoster,
    WorldTowerPreparedCombatant PreparedGuardian);

public sealed record WorldTowerProductionCalibrationReport(
    int SchemaVersion,
    int CanonicalRosterVersion,
    IReadOnlyList<WorldTowerProductionCalibrationResult> Results);

/// <summary>
/// Calibrates World Tower floors through the production snapshot builder,
/// Tower runtime factory, WorldTower Essence loadout, and Tower playback executor.
/// </summary>
public sealed class WorldTowerProductionCalibrationRunner(
    IWorldTowerDefinitionProvider towerDefinitions,
    IEntityService entities,
    IWorldTowerCombatRuntimeFactory runtimeFactory,
    ICombatEngineExecutor combatEngine,
    CanonicalEquipmentBuildFactory canonicalBuilds,
    IEssenceDefinitionRepository essenceDefinitions)
{
    private const int PlaybackCheckpointIntervalTicks = 10;

    public async Task<WorldTowerProductionCalibrationReport> RunAsync(
        WorldTowerProductionCalibrationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new WorldTowerProductionCalibrationOptions();
        if (options.MinimumFloor is < 1 || options.MaximumFloor < options.MinimumFloor)
            throw new ArgumentOutOfRangeException(nameof(options));
        if (options.SampleCount is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(options));

        var floors = towerDefinitions.GetFloors()
            .Where(floor => floor.FloorNumber >= options.MinimumFloor
                            && floor.FloorNumber <= options.MaximumFloor)
            .OrderBy(floor => floor.FloorNumber)
            .ToList();
        if (floors.Count == 0)
            throw new InvalidOperationException("No World Tower floors matched the calibration range.");

        var results = new List<WorldTowerProductionCalibrationResult>();

        foreach (var floor in floors)
        {
            var guardianSource = (await entities.GetEntitiesByIdsForCombatAsync(
                    [floor.GuardianCreatureId],
                    cancellationToken))
                .OfType<Creature>()
                .SingleOrDefault()
                ?? throw new InvalidOperationException(
                    $"Guardian creature '{floor.GuardianCreatureId}' was not found.");
            var cohorts = CreateCohorts(
                floor.RequiredSlots,
                floor.FloorNumber,
                floor.RecommendedPowerRating);

            foreach (var (cohort, loadout) in cohorts)
            {
                var builds = CreateRoster(floor.RequiredSlots, loadout);
                var snapshots = builds.Select((entry, slotIndex) =>
                    CreateSnapshotRequest(entry, slotIndex)).ToArray();
                var outcomes = new List<CombatResult>(options.SampleCount);
                IReadOnlyList<WorldTowerPreparedCombatant>? preparedRoster = null;
                WorldTowerPreparedCombatant? preparedGuardian = null;

                for (var sample = 0; sample < options.SampleCount; sample++)
                {
                    var encounterId = CreateDeterministicGuid(
                        $"tower-calibration:{floor.FloorNumber}:{cohort}:{sample}");
                    var runtime = await runtimeFactory.CreateAsync(
                        new WorldTowerCombatRuntimeRequest(
                            encounterId,
                            CreateDeterministicGuid($"tower-calibration-rally:{floor.FloorNumber}"),
                            floor,
                            snapshots,
                            guardianSource,
                            PlayerDamagePercent: 0,
                            WeakPointPercent: 0,
                            GuardianDamageReductionPercent: 0,
                            StartsAt: DateTimeOffset.UnixEpoch),
                        cancellationToken);

                    if (preparedRoster is null)
                    {
                        preparedRoster = runtime.FriendlyParticipants
                            .Select((participant, index) => CapturePreparedCombatant(
                                participant.Combatant,
                                CombatRatingDisplay.FromRaw(builds[index].Build.Rating.Overall),
                                participant.Slot.PartyNumber))
                            .ToArray();
                        preparedGuardian = CapturePreparedCombatant(
                            runtime.HostileParticipants.Single().Combatant,
                            powerRating: 0);
                    }

                    var execution = await combatEngine.ExecuteTowerPlaybackAsync(
                        runtime,
                        PlaybackCheckpointIntervalTicks,
                        cancellationToken);
                    outcomes.Add(execution.Result);
                }

                results.Add(new WorldTowerProductionCalibrationResult(
                    floor.FloorNumber,
                    cohort,
                    loadout.Rung.Id,
                    loadout.EssenceCount,
                    floor.RequiredSlots,
                    builds.Average(entry => CombatRatingDisplay.FromRaw(entry.Build.Rating.Overall)),
                    outcomes.Count,
                    outcomes.Count(result => result.Outcome == BattleOutcome.Victory) / (double)outcomes.Count,
                    outcomes.Count(result => result.Outcome == BattleOutcome.Draw) / (double)outcomes.Count,
                    outcomes.Average(result => result.Duration),
                    AbilitiesStartOnCooldown: true,
                    preparedRoster!,
                    preparedGuardian!));
            }
        }

        return new WorldTowerProductionCalibrationReport(
            SchemaVersion: 1,
            CanonicalCooperativeRosterCatalog.Version,
            results);
    }

    private IReadOnlyList<(CanonicalCooperativeRosterSlot Slot, CanonicalEquipmentBuild Build)> CreateRoster(
        int rosterSize,
        CalibrationLoadout loadout) =>
        CanonicalCooperativeRosterCatalog.CreateParty(rosterSize)
            .Select(slot => (
                slot,
                canonicalBuilds.CreateBuild(
                    slot.Role,
                    loadout.Rung,
                    loadout.EssenceCount)))
            .ToArray();

    private IReadOnlyList<(WorldTowerCalibrationCohort Cohort, CalibrationLoadout Loadout)>
        CreateCohorts(
            int rosterSize,
            int floorNumber,
            int recommendedPowerRating)
    {
        if (floorNumber >= 11)
        {
            var lateRecommended = CreateLoadout(
                WorldTowerEquipmentRequirementCurve.Get(floorNumber));
            var lateBelow = floorNumber == WorldTowerEquipmentRequirementCurve.FirstFloor
                ? new CalibrationLoadout(
                    GetRung(2, Rarity.Epic, ItemQuality.Standard),
                    EssenceCount: 7)
                : CreateLoadout(WorldTowerEquipmentRequirementCurve.Get(floorNumber - 1));
            var lateStronger = CreateLoadout(WorldTowerEquipmentRequirementCurve.Get(
                Math.Min(WorldTowerEquipmentRequirementCurve.FinalFloor, floorNumber + 1)));
            return
            [
                (WorldTowerCalibrationCohort.BelowRecommended, lateBelow),
                (WorldTowerCalibrationCohort.Recommended, lateRecommended),
                (WorldTowerCalibrationCohort.Stronger, lateStronger)
            ];
        }

        var recommended = FindClosestLoadout(
            rosterSize,
            recommendedPowerRating,
            maximumTier: 1,
            requiredEssenceCount: floorNumber <= 3 ? 5 : floorNumber <= 6 ? 6 : 7,
            requiredQuality: ItemQuality.Standard);
        var below = FindAdjacentLoadout(
            rosterSize,
            recommended,
            minimumRatingDifference: 15,
            maximumEssenceCount: 7,
            below: true);
        var stronger = FindAdjacentLoadout(
            rosterSize,
            recommended,
            minimumRatingDifference: 15,
            maximumEssenceCount: 7,
            below: false);
        return
        [
            (WorldTowerCalibrationCohort.BelowRecommended, below),
            (WorldTowerCalibrationCohort.Recommended, recommended),
            (WorldTowerCalibrationCohort.Stronger, stronger)
        ];
    }

    private CalibrationLoadout FindClosestLoadout(
        int rosterSize,
        int targetPowerRating,
        int maximumTier,
        int? requiredEssenceCount = null,
        ItemQuality? requiredQuality = null)
    {
        var candidates = GetLoadoutCandidates(rosterSize, maximumTier)
            .Where(candidate => requiredEssenceCount is null
                                || candidate.Loadout.EssenceCount == requiredEssenceCount)
            .Where(candidate => requiredQuality is null
                                || candidate.Loadout.Rung.Quality == requiredQuality)
            .ToArray();
        return candidates
            .OrderBy(candidate => Math.Abs(candidate.Average - targetPowerRating))
            .ThenBy(candidate => candidate.Average > targetPowerRating)
            .ThenBy(candidate => candidate.Loadout.Rung.Index)
            .ThenBy(candidate => candidate.Loadout.EssenceCount)
            .First().Loadout;
    }

    private CalibrationLoadout FindAdjacentLoadout(
        int rosterSize,
        CalibrationLoadout recommended,
        int minimumRatingDifference,
        int maximumEssenceCount,
        bool below)
    {
        var adjacentEssenceCount = Math.Clamp(
            recommended.EssenceCount + (below ? -2 : 2),
            1,
            maximumEssenceCount);
        if (adjacentEssenceCount != recommended.EssenceCount)
            return recommended with { EssenceCount = adjacentEssenceCount };

        var candidates = GetLoadoutCandidates(rosterSize, maximumTier: 1);
        var recommendedRating = AverageRating(rosterSize, recommended);
        var qualified = FilterAdjacent(candidates.Where(candidate =>
            candidate.Loadout.EssenceCount == recommended.EssenceCount));
        return (below
                ? qualified.OrderByDescending(candidate => candidate.Average)
                : qualified.OrderBy(candidate => candidate.Average))
            .ThenBy(candidate => candidate.Loadout.Rung.Index)
            .ThenBy(candidate => candidate.Loadout.EssenceCount)
            .First().Loadout;

        IEnumerable<CalibrationLoadoutCandidate> FilterAdjacent(
            IEnumerable<CalibrationLoadoutCandidate> source) => below
            ? source.Where(candidate =>
                candidate.Average <= recommendedRating - minimumRatingDifference)
            : source.Where(candidate =>
                candidate.Average >= recommendedRating + minimumRatingDifference);
    }

    private IReadOnlyList<CalibrationLoadoutCandidate> GetLoadoutCandidates(
        int rosterSize,
        int maximumTier)
    {
        var roles = CanonicalCooperativeRosterCatalog.CreateParty(rosterSize);
        return canonicalBuilds.GetProgressionLadder()
            .Where(rung => rung.Tier <= maximumTier)
            .SelectMany(rung => Enumerable.Range(
                1,
                CanonicalEquipmentBuildFactory.MaximumCanonicalEssenceCount),
                (rung, essenceCount) => new CalibrationLoadout(rung, essenceCount))
            .Select(loadout => new CalibrationLoadoutCandidate(
                loadout,
                roles.Average(slot => CombatRatingDisplay.FromRaw(canonicalBuilds.CreateBuild(
                    slot.Role,
                    loadout.Rung,
                    loadout.EssenceCount).Rating.Overall))))
            .ToArray();
    }

    private double AverageRating(int rosterSize, CalibrationLoadout loadout) =>
        CanonicalCooperativeRosterCatalog.CreateParty(rosterSize)
            .Average(slot => CombatRatingDisplay.FromRaw(canonicalBuilds.CreateBuild(
                slot.Role,
                loadout.Rung,
                loadout.EssenceCount).Rating.Overall));

    private CanonicalEquipmentProgressionRung GetRung(
        int tier,
        Rarity rarity,
        ItemQuality quality) =>
        canonicalBuilds.GetProgressionLadder().Single(rung =>
            rung.Tier == tier && rung.Rarity == rarity && rung.Quality == quality);

    private CalibrationLoadout CreateLoadout(WorldTowerEquipmentRequirement requirement) =>
        new(
            GetRung(requirement.Tier, requirement.Rarity, requirement.Quality),
            requirement.EssenceCount);

    private SnapshotCombatantRequest CreateSnapshotRequest(
        (CanonicalCooperativeRosterSlot Slot, CanonicalEquipmentBuild Build) entry,
        int slotIndex)
    {
        var snapshotId = CreateDeterministicGuid(
            $"tower-calibration-snapshot:{entry.Build.Rung.Id}:{slotIndex}");
        var characterId = CreateDeterministicGuid(
            $"tower-calibration-character:{entry.Build.Rung.Id}:{slotIndex}");
        var snapshot = new CharacterSnapshot
        {
            Id = snapshotId,
            CharacterId = characterId,
            Name = $"Calibration {entry.Slot.Role} {slotIndex + 1}",
            Level = entry.Build.Character.Level,
            BaseAttributes = entry.Build.Character.BaseAttributes.Select(attribute =>
                new EntityAttributeSnapshot
                {
                    CharacterSnapshotId = snapshotId,
                    AttributeType = attribute.AttributeType,
                    Value = attribute.Value
                }).ToArray(),
            Equipment = entry.Build.Equipment.Select(equipment =>
                EquipmentSnapshot.From(ToSlot(equipment.EquipmentBase.EquipmentType), equipment)).ToArray(),
            EquippedEssences = entry.Build.EquippedEssences.Select((essence, essenceIndex) =>
                EquippedEssenceSnapshot.From(snapshotId, essenceIndex, essence)).ToArray()
        };
        return new SnapshotCombatantRequest(
            snapshot,
            new CombatParticipantSlot(
                characterId.ToString(),
                characterId,
                CombatSide.Friendly,
                entry.Slot.PartyNumber));
    }

    private WorldTowerPreparedCombatant CapturePreparedCombatant(
        CombatEntity combatant,
        int powerRating,
        int? partyNumber = null)
    {
        var abilityIds = combatant.NativeAbilityIds
            .Concat(combatant.EquippedEssences.SelectMany(essence =>
            {
                var definition = essenceDefinitions.GetById(essence.EssenceDefinitionId);
                return definition is null
                    ? []
                    : new[] { definition.ActiveAbility.Id, definition.PassiveAbility.Id };
            }))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var equipment = combatant.Equipment.Select(item => new WorldTowerPreparedEquipment(
            item.ItemBaseId,
            item.EquipmentBase.EquipmentType,
            item.Tier,
            item.Rarity,
            item.Quality,
            item.BaseRecipeId,
            item.BlueprintId,
            item.EquipmentSetId,
            item.AttributeModifiers.Select(modifier => new WorldTowerPreparedEquipmentModifier(
                modifier.AttributeType,
                modifier.Amount,
                modifier.ModifierType)).ToArray())).ToArray();

        return new WorldTowerPreparedCombatant(
            combatant.Id,
            combatant.Name,
            combatant.Level,
            partyNumber,
            powerRating,
            Enum.GetValues<AttributeType>().ToDictionary(
                attribute => attribute,
                combatant.GetAttributeValue),
            equipment,
            equipment.Select(item => item.EquipmentSetId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            combatant.EquippedEssences.Select(essence => essence.EssenceDefinitionId).ToArray(),
            combatant.Tags.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            abilityIds);
    }

    private static EquipmentSlotType ToSlot(EquipmentType type) => type switch
    {
        EquipmentType.Head => EquipmentSlotType.Head,
        EquipmentType.Relic => EquipmentSlotType.Relic,
        EquipmentType.Chest => EquipmentSlotType.Chest,
        EquipmentType.Necklace => EquipmentSlotType.Necklace,
        EquipmentType.Legs => EquipmentSlotType.Legs,
        EquipmentType.Ring => EquipmentSlotType.Ring,
        EquipmentType.OneHanded or EquipmentType.TwoHanded => EquipmentSlotType.MainHand,
        EquipmentType.OffHand => EquipmentSlotType.OffHand,
        EquipmentType.Tool => EquipmentSlotType.Tool,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private static Guid CreateDeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }

    private sealed record CalibrationLoadout(
        CanonicalEquipmentProgressionRung Rung,
        int EssenceCount);

    private sealed record CalibrationLoadoutCandidate(
        CalibrationLoadout Loadout,
        double Average);
}
