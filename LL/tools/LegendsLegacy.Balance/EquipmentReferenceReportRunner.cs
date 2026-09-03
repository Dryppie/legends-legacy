using System.Globalization;
using Common.Randomness;
using Domain.Models.Combat;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.PowerRatings;

namespace LegendsLegacy.Balance;

public sealed record EquipmentReferenceItemSnapshot(
    IReadOnlyList<EquipmentSlotType> Slots, EquipmentData Descriptor);

public sealed record EquipmentReferenceCombatSnapshot(
    int Seed, string OpponentId, string Outcome, int DurationTicks,
    int DamageDealt, int DamageTaken, int HealingDone);

public sealed record EquipmentReferenceBuildSnapshot(
    EquipmentReferenceBuildDefinition Definition, int EquipmentBalanceVersion,
    GearPackageCombatRatingSnapshot CombatRating,
    IReadOnlyDictionary<string, float> PreparedAttributes,
    IReadOnlyList<EquipmentReferenceItemSnapshot> Equipment,
    EquipmentReferenceCombatSnapshot Combat);

public sealed record EquipmentReferenceReport(
    int ReportVersion, int Seed, string Purpose, string FixtureSha256,
    IReadOnlyList<EquipmentReferenceBuildSnapshot> Builds);

public sealed class EquipmentReferenceReportRunner(
    EquipmentReferenceBuildFactory builds, ICombatSetupService combatSetup, ICombatEngineExecutor combatEngine)
{
    public const string FixtureFileName = "equipment-reference-builds.v1.json";
    public const int MaximumCombatTicks = 1800;
    public const string Purpose = "Equipment progression Tier 1 implementation reference; rank/style comparisons and production combat smoke checks. " +
        "The fixed rank-0 opponent is synthetic. No content balance or release certification is implied.";

    public async Task<EquipmentReferenceReport> RunAsync(
        IReadOnlyList<EquipmentReferenceBuildDefinition> profiles, int seed, string fixtureSha256,
        CancellationToken cancellationToken = default, bool regionTwoTransition = false)
    {
        if (profiles.Count == 0 || profiles.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != profiles.Count
            || profiles.Any(x => x.Tier < 1 || x.Rank != 0))
            throw new ArgumentException("Reference profiles must have distinct IDs and describe supported tiers at rank 0.", nameof(profiles));
        var baseline = profiles.Single(x => x.Id == "balanced-plain");
        if (profiles.Any(x => x.CharacterLevel != baseline.CharacterLevel))
            throw new ArgumentException("Reference comparisons require a common character level.", nameof(profiles));
        // Validate the complete matrix before starting potentially lengthy combat work.
        var matrix = profiles.SelectMany(profile => Enumerable.Range(0, EquipmentBalance.MaximumRank + 1)
            .Select(rank => builds.Create(profile with { Id = $"{profile.Id}-rank-{rank}", Rank = rank }))).ToArray();
        var snapshots = new List<EquipmentReferenceBuildSnapshot>();
        foreach (var build in matrix)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var opponent = builds.Create(baseline with { Id = regionTwoTransition ? "fixed-tier1-rank5-opponent" : "fixed-balanced-rank-0-opponent", Rank = regionTwoTransition ? 5 : 0 });
            var friendly = Prepare(build);
            var hostile = Prepare(opponent);
            await combatSetup.PrepareEntitiesForCombat([friendly, hostile], EssenceCombatActivity.Arena);
            var attributes = friendly.CombatAttributes.OrderBy(x => x.Key)
                .ToDictionary(x => x.Key.ToString(), x => x.Value, StringComparer.Ordinal);
            var combatSeed = StableRandom.Seed(EquipmentKeys.ReferenceCombatIdentity, seed.ToString(CultureInfo.InvariantCulture));
            var encounterId = StableRandom.Guid(EquipmentKeys.ReferenceEncounterIdentity, build.Definition.Id, combatSeed.ToString(CultureInfo.InvariantCulture));
            var friendlySlot = new CombatParticipantSlot(friendly.Id, build.Character.Id, CombatSide.Friendly);
            var hostileSlot = new CombatParticipantSlot(hostile.Id, opponent.Character.Id, CombatSide.Hostile);
            var plan = new CombatEncounterPlan(encounterId, CombatMode.Pvp, 1, DateTimeOffset.UnixEpoch,
                [friendlySlot, hostileSlot], new PvpEncounterSourceContext(encounterId, build.Character.Id, opponent.Character.Id))
            {
                ContentType = CombatContentType.Arena, RandomSeed = combatSeed, CaptureEventLog = false
            };
            var runtime = new CombatEncounterRuntime(plan,
                [new(friendlySlot, build.Character, friendly)], [new(hostileSlot, opponent.Character, hostile)]);
            var result = await combatEngine.ExecuteSimulationAsync(runtime,
                new CombatRuleset(combatSeed, MaximumCombatTicks, CaptureEventLog: false), cancellationToken);
            var stats = result.EntityStats.Single(x => x.EntityId == friendly.Id);
            snapshots.Add(new(build.Definition, build.EquipmentBalanceVersion,
                GearPackageFactory.CreateRatingSnapshot(build.Rating), attributes,
                build.Equipment.Select(item => new EquipmentReferenceItemSnapshot(
                    build.Character.EquipmentSlots.Where(slot => slot.EquipmentInstanceId == item.Id)
                        .Select(slot => slot.EquipmentSlotType).Order().ToArray(), item.ProgressionData!)).ToArray(),
                new(combatSeed, opponent.Definition.Id, result.Outcome.ToString(), result.Duration,
                    stats.DamageDone, stats.DamageTaken, stats.HealingDone)));
        }
        return new(1, seed, regionTwoTransition ? "Level-50 Tier 1 / Tier 2 transition against a synthetic Tier 1 rank-5 opponent; production combat smoke checks, not real-area balance certification." : Purpose, fixtureSha256, snapshots);
    }

    private static CombatEntity Prepare(EquipmentReferenceBuild build) => new(build.Character)
    {
        EquippedEssences = [.. build.EquippedEssences], HasEquippedEssenceSnapshot = true
    };
}
