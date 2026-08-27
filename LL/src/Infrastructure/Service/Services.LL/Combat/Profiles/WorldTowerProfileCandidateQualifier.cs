using System.Security.Cryptography;
using System.Text;
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

    Task<WorldTowerCalibrationAnchorSearchResult> SearchCalibrationAnchorsAsync(
        IReadOnlyList<AbilityBalanceEssenceResult> essenceResults,
        CombatCharacterProfileScenario scenario,
        IReadOnlyList<int> floorNumbers,
        int sampleCount,
        int baseRandomSeed,
        IReadOnlySet<string> excludedSignatures,
        CancellationToken cancellationToken);
}

public sealed record WorldTowerCalibrationAnchorSearchResult(
    IReadOnlyList<AbilityBalanceCombinationResult> Candidates,
    IReadOnlyDictionary<string, IReadOnlyList<CombatCharacterProfileContextEvidence>> ContextEvidence);

/// <summary>
/// Qualifies generic Essence finalists against the exact authored Tower floors through
/// production preparation, party assignment, stagger runtime, and playback execution.
/// The bounded qualification phase is intentionally much smaller than discovery.
/// </summary>
public sealed class WorldTowerProfileCandidateQualifier(
    CombatCharacterProfileMaterializer materializer,
    CanonicalEquipmentBuildFactory canonicalBuilds,
    IEssenceDefinitionRepository essenceDefinitions,
    Application.Interfaces.Services.LL.WorldTower.IWorldTowerDefinitionProvider towerDefinitions,
    IEntityService entities,
    IWorldTowerCombatRuntimeFactory runtimeFactory,
    ICombatEngineExecutor combatEngine) : IWorldTowerProfileCandidateQualifier
{
    public const int ContractVersion = 10;
    public const int CalibrationPortfolioTeamCount = 4;
    private const int PlaybackCheckpointIntervalTicks = 10;
    private const int AnchorCandidatePoolSize = 500;
    private const int AnchorQualificationBatchSize = 20;

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
                sampleCount == WorldTowerProfileTargetContract.SelectionConfirmationSampleCount
                    ? WorldTowerProfileTargetContract.CertificationSeedManifestId
                    : $"world-tower-profile-qualification-v{ContractVersion}:{scenario.Id}:floor-{floor.FloorNumber}",
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

    public async Task<WorldTowerCalibrationAnchorSearchResult> SearchCalibrationAnchorsAsync(
        IReadOnlyList<AbilityBalanceEssenceResult> essenceResults,
        CombatCharacterProfileScenario scenario,
        IReadOnlyList<int> floorNumbers,
        int sampleCount,
        int baseRandomSeed,
        IReadOnlySet<string> excludedSignatures,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(essenceResults);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(floorNumbers);
        ArgumentNullException.ThrowIfNull(excludedSignatures);

        var requiredFloors = floorNumbers.Distinct().Order().ToArray();
        var uncoveredFloors = requiredFloors.ToHashSet();
        if (requiredFloors.Length == 0)
        {
            return new WorldTowerCalibrationAnchorSearchResult(
                [],
                new Dictionary<string, IReadOnlyList<CombatCharacterProfileContextEvidence>>(
                    StringComparer.Ordinal));
        }

        var candidates = CreateAnchorCandidates(
                essenceResults,
                scenario,
                baseRandomSeed)
            .Where(candidate => !excludedSignatures.Contains(candidate.Signature))
            .ToArray();
        var selected = new List<AbilityBalanceCombinationResult>();
        var selectedEvidence = new Dictionary<string, IReadOnlyList<CombatCharacterProfileContextEvidence>>(
            StringComparer.Ordinal);
        var rejectedSignatures = new HashSet<string>(StringComparer.Ordinal);

        for (var offset = 0;
             offset < candidates.Length
             && (selected.Count < CalibrationPortfolioTeamCount || uncoveredFloors.Count > 0);
             offset += AnchorQualificationBatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = candidates.Skip(offset).Take(AnchorQualificationBatchSize).ToArray();
            var evidence = await QualifyAsync(
                batch,
                scenario,
                sampleCount,
                baseRandomSeed,
                cancellationToken);

            var matches = batch
                .Where(candidate => !rejectedSignatures.Contains(candidate.Signature))
                .Select(candidate => new
                {
                    Candidate = candidate,
                    Evidence = evidence.GetValueOrDefault(candidate.Signature) ?? [],
                })
                .Select(candidate => new
                {
                    candidate.Candidate,
                    candidate.Evidence,
                    Floors = candidate.Evidence
                        .Where(item => requiredFloors.Contains(item.FloorNumber)
                                       && WorldTowerProfileTargetContract.Contains(item.WinRate))
                        .Select(item => item.FloorNumber)
                        .Distinct()
                        .Order()
                        .ToArray()
                })
                .Where(candidate => requiredFloors.All(floorNumber =>
                    candidate.Evidence.Any(item => item.FloorNumber == floorNumber
                                                   && WorldTowerProfileTargetContract.IsBelowMaximum(item.WinRate))))
                .OrderByDescending(candidate => candidate.Floors.Count(uncoveredFloors.Contains))
                .ThenBy(candidate => candidate.Floors.Length == 0
                    ? double.MaxValue
                    : TargetDistance(candidate.Evidence, candidate.Floors))
                .ThenBy(candidate => candidate.Candidate.Signature, StringComparer.Ordinal)
                .ToArray();
            foreach (var match in matches)
            {
                if (selected.Count >= CalibrationPortfolioTeamCount && uncoveredFloors.Count == 0)
                    break;

                var confirmed = await QualifyAsync(
                    [match.Candidate],
                    scenario,
                    WorldTowerProfileTargetContract.SelectionConfirmationSampleCount,
                    baseRandomSeed,
                    cancellationToken);
                var confirmedEvidence = confirmed[match.Candidate.Signature];
                var confirmedFloors = confirmedEvidence
                    .Where(item => requiredFloors.Contains(item.FloorNumber)
                                   && WorldTowerProfileTargetContract.Contains(item.WinRate))
                    .Select(item => item.FloorNumber)
                    .Distinct()
                    .Order()
                    .ToArray();
                var allFloorsBelowMaximum = requiredFloors.All(floorNumber =>
                    confirmedEvidence.Any(item => item.FloorNumber == floorNumber
                                                  && WorldTowerProfileTargetContract.IsBelowMaximum(item.WinRate)));
                if (!allFloorsBelowMaximum)
                {
                    rejectedSignatures.Add(match.Candidate.Signature);
                    continue;
                }
                if (selected.Count >= CalibrationPortfolioTeamCount
                    && !confirmedFloors.Any(uncoveredFloors.Contains))
                    continue;

                selected.Add(match.Candidate);
                selectedEvidence.Add(match.Candidate.Signature, confirmedEvidence);
                foreach (var floorNumber in confirmedFloors)
                    uncoveredFloors.Remove(floorNumber);
            }
        }

        return new WorldTowerCalibrationAnchorSearchResult(selected, selectedEvidence);
    }

    private IReadOnlyList<AbilityBalanceCombinationResult> CreateAnchorCandidates(
        IReadOnlyList<AbilityBalanceEssenceResult> essenceResults,
        CombatCharacterProfileScenario scenario,
        int baseRandomSeed)
    {
        var scores = essenceResults
            .GroupBy(result => result.EssenceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().AdjustedScoreDelta,
                StringComparer.OrdinalIgnoreCase);
        var families = essenceDefinitions.GetAll()
            .Where(definition => !string.IsNullOrWhiteSpace(definition.Id)
                                 && !string.IsNullOrWhiteSpace(definition.SourceMonsterId)
                                 && !definition.Id.Equals("essence.training", StringComparison.OrdinalIgnoreCase))
            .GroupBy(definition => definition.SourceMonsterId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderBy(definition => scores.GetValueOrDefault(definition.Id))
                .ThenBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray())
            .Where(group => group.Length > 0)
            .OrderBy(group => group.Average(definition => scores.GetValueOrDefault(definition.Id)))
            .ThenBy(group => group[0].SourceMonsterId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (families.Length < scenario.EssencesPerParticipant)
        {
            throw new InvalidOperationException(
                $"World Tower direct anchor search requires {scenario.EssencesPerParticipant} distinct Essence "
                + $"sources per participant, but only {families.Length} are available.");
        }

        var roles = CanonicalCooperativeRosterCatalog.CreateParty(scenario.DiscoveryTeamSize)
            .Select(slot => slot.Role.ToString())
            .ToArray();
        var candidates = new List<AbilityBalanceCombinationResult>(AnchorCandidatePoolSize);
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        for (var candidateIndex = 0; candidateIndex < AnchorCandidatePoolSize; candidateIndex++)
        {
            var participants = roles.Select((role, participantIndex) =>
            {
                var seed = StableSeed(
                    $"{ContractVersion}:{scenario.Id}:{baseRandomSeed}:{candidateIndex}:{participantIndex}");
                var random = new Random(seed);
                var bandSize = Math.Clamp(
                    scenario.EssencesPerParticipant + candidateIndex / 25,
                    scenario.EssencesPerParticipant,
                    families.Length);
                var eligibleFamilies = candidateIndex < 250
                    ? families.Take(bandSize).ToArray()
                    : families;
                var selectedFamilies = eligibleFamilies
                    .OrderBy(_ => random.Next())
                    .Take(scenario.EssencesPerParticipant)
                    .ToArray();
                var ids = selectedFamilies
                    .Select(family => family[random.Next(family.Length)].Id)
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                return new AbilityBalanceParticipantLoadout(ids, role);
            }).ToArray();
            var canonical = string.Join('|', participants.Select(participant =>
                $"{participant.Role}:{string.Join(',', participant.EssenceIds)}"));
            var signature = $"tower-anchor:v{ContractVersion}:{Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant()[..24]}";
            if (!signatures.Add(signature))
                continue;
            candidates.Add(new AbilityBalanceCombinationResult(
                signature,
                $"Direct Tower anchor candidate {candidateIndex + 1}",
                participants,
                0,
                0,
                0,
                0,
                0d,
                0d,
                0d,
                0d,
                0d,
                0d,
                []));
        }
        return candidates;
    }

    private static double TargetDistance(
        IReadOnlyList<CombatCharacterProfileContextEvidence> evidence,
        IReadOnlyCollection<int> floors)
    {
        var midpoint = (WorldTowerProfileTargetContract.MinimumWinRate
                        + WorldTowerProfileTargetContract.MaximumWinRate) / 2d;
        return evidence.Where(item => floors.Contains(item.FloorNumber))
            .Average(item => Math.Abs(item.WinRate - midpoint));
    }

    private static int StableSeed(string value) => BitConverter.ToInt32(
        SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
