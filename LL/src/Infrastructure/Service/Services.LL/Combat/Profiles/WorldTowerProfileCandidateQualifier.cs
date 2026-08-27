using Application.Interfaces.Services.LL.CombatProfiles;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.WorldTower;
using Services.LL.PowerRatings;

namespace Services.LL.Combat.Profiles;

public interface IWorldTowerProfileCandidateQualifier
{
    Task<IReadOnlyDictionary<string, IReadOnlyList<CombatCharacterProfileContextEvidence>>>
        QualifyAsync(
            IReadOnlyList<AbilityBalanceCombinationResult> candidates,
            CombatCharacterProfileScenario scenario,
            int sampleCount,
            int baseRandomSeed,
            CancellationToken cancellationToken);
}

/// <summary>
/// Qualifies generic Essence finalists against the exact authored Tower floors through
/// production preparation, party assignment, stagger runtime, and playback execution.
/// The bounded qualification phase is intentionally much smaller than discovery.
/// </summary>
public sealed class WorldTowerProfileCandidateQualifier(
    CombatCharacterProfileMaterializer materializer,
    CanonicalEquipmentBuildFactory canonicalBuilds,
    Application.Interfaces.Services.LL.WorldTower.IWorldTowerDefinitionProvider towerDefinitions,
    IEntityService entities,
    IWorldTowerCombatRuntimeFactory runtimeFactory,
    ICombatEngineExecutor combatEngine) : IWorldTowerProfileCandidateQualifier
{
    public const int ContractVersion = 1;
    private const int PlaybackCheckpointIntervalTicks = 10;

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<CombatCharacterProfileContextEvidence>>>
        QualifyAsync(
            IReadOnlyList<AbilityBalanceCombinationResult> candidates,
            CombatCharacterProfileScenario scenario,
            int sampleCount,
            int baseRandomSeed,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(scenario);
        if (sampleCount is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(sampleCount));
        var floorNumbers = (scenario.FloorNumbers ?? [])
            .Distinct()
            .Order()
            .ToArray();
        if (floorNumbers.Length == 0)
            return new Dictionary<string, IReadOnlyList<CombatCharacterProfileContextEvidence>>(
                StringComparer.Ordinal);

        var floors = towerDefinitions.GetFloors()
            .Where(floor => floorNumbers.Contains(floor.FloorNumber))
            .OrderBy(floor => floor.FloorNumber)
            .ToArray();
        if (floors.Length != floorNumbers.Length)
            throw new InvalidOperationException("Profile qualification could not resolve every requested Tower floor.");
        if (floors.Any(floor => floor.RequiredSlots != scenario.TeamSize))
            throw new InvalidOperationException("Profile qualification floors do not match the scenario roster size.");

        var guardianIds = floors.Select(floor => floor.GuardianCreatureId).Distinct().ToArray();
        var guardians = (await entities.GetEntitiesByIdsForCombatAsync(guardianIds.ToList(), cancellationToken))
            .OfType<Creature>()
            .ToDictionary(creature => creature.Id);
        var rarity = Enum.Parse<Rarity>(scenario.EquipmentRarity, true);
        var quality = Enum.Parse<ItemQuality>(scenario.EquipmentQuality, true);
        var rung = canonicalBuilds.GetProgressionLadder().Single(candidate =>
            candidate.Tier == scenario.EquipmentTier
            && candidate.Rarity == rarity
            && candidate.Quality == quality);
        var slots = CanonicalCooperativeRosterCatalog.CreateParty(scenario.TeamSize);
        var partyRoles = CanonicalCooperativeRosterCatalog.CreateParty(scenario.DiscoveryTeamSize)
            .Select(slot => slot.Role.ToString())
            .ToArray();
        var manifests = floors.ToDictionary(
            floor => floor.FloorNumber,
            floor => WorldTowerCalibrationSeedManifest.Create(
                $"world-tower-profile-qualification-v{ContractVersion}:{scenario.Id}:floor-{floor.FloorNumber}",
                baseRandomSeed,
                sampleCount));
        var results = new Dictionary<string, IReadOnlyList<CombatCharacterProfileContextEvidence>>(
            StringComparer.Ordinal);

        foreach (var candidate in candidates.OrderBy(candidate => candidate.Signature, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (candidate.Participants.Count != scenario.DiscoveryTeamSize)
                throw new InvalidOperationException("Tower qualification requires complete five-character finalists.");
            for (var partySlot = 0; partySlot < candidate.Participants.Count; partySlot++)
            {
                if (!candidate.Participants[partySlot].Role.Equals(
                        partyRoles[partySlot],
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Finalist '{candidate.Signature}' does not preserve canonical role slot {partySlot + 1}.");
                }
            }

            var teamId = CombatCharacterProfileIdentity.CreateStableId(
                "qualification-team",
                scenario.Id,
                candidate.Signature,
                baseRandomSeed.ToString());
            var profiles = await materializer.MaterializeTeamAsync(
                slots.Select(slot =>
                {
                    var partySlotIndex = slot.SlotIndex % scenario.DiscoveryTeamSize;
                    var participant = candidate.Participants[partySlotIndex];
                    var id = CombatCharacterProfileIdentity.CreateStableId(
                        "qualification-profile",
                        teamId,
                        slot.SlotIndex.ToString());
                    return new CombatCharacterProfileMaterializationRequest(
                        id,
                        teamId,
                        slot.SlotIndex,
                        $"Qualification {slot.Role} {slot.SlotIndex + 1}",
                        "Qualification",
                        slot.Role.ToString(),
                        CombatContentType.WorldTower,
                        CanonicalCooperativeRosterCatalog.EquipmentProfileFor(slot.Role),
                        rung,
                        participant.EssenceIds,
                        slot.PartyNumber,
                        partySlotIndex,
                        CombatCharacterProfileIdentity.CreateStableId(
                            "qualification-party",
                            teamId,
                            slot.PartyNumber.ToString()));
                }).ToArray(),
                cancellationToken);
            var snapshots = profiles
                .OrderBy(profile => profile.SlotIndex)
                .Select(profile => materializer.CreateSnapshotRequest(profile))
                .ToArray();
            var evidence = new List<CombatCharacterProfileContextEvidence>(floors.Length);

            foreach (var floor in floors)
            {
                if (!guardians.TryGetValue(floor.GuardianCreatureId, out var guardian))
                {
                    throw new InvalidOperationException(
                        $"Guardian creature '{floor.GuardianCreatureId}' was not found.");
                }
                var manifest = manifests[floor.FloorNumber];
                var outcomes = new List<CombatResult>(sampleCount);
                for (var sample = 0; sample < sampleCount; sample++)
                {
                    var runtime = await runtimeFactory.CreateAsync(
                        new WorldTowerCombatRuntimeRequest(
                            CombatCharacterProfileIdentity.CreateDeterministicGuid(
                                $"tower-profile-qualification:{scenario.Id}:{floor.FloorNumber}:{candidate.Signature}:{sample}"),
                            CombatCharacterProfileIdentity.CreateDeterministicGuid(
                                $"tower-profile-qualification-rally:{scenario.Id}:{floor.FloorNumber}"),
                            floor,
                            snapshots,
                            guardian,
                            PlayerDamagePercent: 0,
                            WeakPointPercent: 0,
                            GuardianDamageReductionPercent: 0,
                            StartsAt: DateTimeOffset.UnixEpoch,
                            RandomSeed: manifest.Seeds[sample]),
                        cancellationToken);
                    var execution = await combatEngine.ExecuteTowerPlaybackAsync(
                        runtime,
                        PlaybackCheckpointIntervalTicks,
                        cancellationToken);
                    outcomes.Add(execution.Result);
                }

                evidence.Add(new CombatCharacterProfileContextEvidence(
                    scenario.Id,
                    floor.FloorNumber,
                    scenario.TeamSize,
                    outcomes.Count,
                    outcomes.Count(outcome => outcome.Outcome == BattleOutcome.Victory),
                    outcomes.Count(outcome => outcome.Outcome == BattleOutcome.Defeat),
                    outcomes.Count(outcome => outcome.Outcome == BattleOutcome.Draw),
                    outcomes.Count(outcome => outcome.Outcome == BattleOutcome.Victory) / (double)outcomes.Count,
                    outcomes.Count(outcome => outcome.Outcome == BattleOutcome.Draw) / (double)outcomes.Count,
                    outcomes.Average(outcome => outcome.Duration),
                    manifest.Id,
                    manifest.Hash,
                    UsesProductionRuntime: true,
                    AbilitiesStartOnCooldown: true));
            }

            results.Add(candidate.Signature, evidence);
        }

        return results;
    }
}
