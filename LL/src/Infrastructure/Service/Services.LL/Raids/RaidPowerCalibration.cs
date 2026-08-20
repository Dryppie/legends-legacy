using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.Raids;
using Common.Randomness;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Professions.Crafting.V2;
using Domain.Models.Raids;
using Domain.Models.Snapshots;
using Microsoft.Extensions.Options;
using Services.LL.PowerRatings;

namespace Services.LL.Raids;

public sealed class RaidPowerCalibrationOptions
{
    public const string SectionName = "RaidPowerCalibration";
    public bool Enabled { get; init; }
    public int SampleCount { get; init; } = 30;
}

public sealed class RaidPowerRecommendationStore : IRaidPowerRecommendationStore
{
    private IReadOnlyDictionary<string, RaidPowerRecommendation> recommendations =
        new Dictionary<string, RaidPowerRecommendation>(StringComparer.OrdinalIgnoreCase);

    public bool IsCalibrationComplete { get; private set; }

    public bool TryGet(string raidBossId, int tier, out RaidPowerRecommendation recommendation) =>
        recommendations.TryGetValue(Key(raidBossId, tier), out recommendation!);

    public void Publish(IReadOnlyDictionary<string, RaidPowerRecommendation> values) =>
        Interlocked.Exchange(ref recommendations,
            new Dictionary<string, RaidPowerRecommendation>(values, StringComparer.OrdinalIgnoreCase));

    public void MarkCalibrationComplete() => IsCalibrationComplete = true;

    public static string Key(string raidBossId, int tier) => $"{raidBossId}:{tier}";
}

public sealed class RaidPowerAnalyzer(
    IRaidBossDefinitionProvider definitions,
    CanonicalEquipmentBuildFactory canonicalBuilds,
    IRaidCombatResolver resolver,
    IOptions<RaidPowerCalibrationOptions> options,
    JsonSerializerOptions jsonOptions,
    TimeProvider timeProvider) : IRaidPowerAnalyzer
{
    public const int SeedSetVersion = 1;
    private const decimal ClearTarget = 0.85m;

    public RaidPowerCalibrationIdentity GetIdentity(string raidBossId, int tier)
    {
        var definition = GetTier(raidBossId, tier);
        var json = JsonSerializer.Serialize(definition, jsonOptions);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
        return new RaidPowerCalibrationIdentity(
            raidBossId,
            tier,
            hash,
            RaidRules.Version,
            PowerRatingAlgorithm.Version,
            PowerRatingAlgorithm.CombatRulesVersion,
            EquipmentStatBudgetCatalog.BalanceVersion,
            SeedSetVersion);
    }

    public async Task<RaidPowerRecommendation> AnalyzeAsync(
        string raidBossId,
        int tier,
        CancellationToken cancellationToken)
    {
        var boss = definitions.Get(raidBossId)
            ?? throw new InvalidOperationException($"Raid boss '{raidBossId}' was not found.");
        var tierDefinition = GetTier(raidBossId, tier);
        var ladder = canonicalBuilds.GetProgressionLadder();
        var sampleCount = Math.Clamp(options.Value.SampleCount, 10, 100);
        var evaluations = new Dictionary<int, Evaluation>();

        async Task<Evaluation> EvaluateAsync(int index)
        {
            if (evaluations.TryGetValue(index, out var cached))
                return cached;
            var rung = ladder[index];
            var roster = CreateRoster(boss, tierDefinition, rung);
            var samples = await resolver.PreviewAsync(roster.Run, tierDefinition, sampleCount, cancellationToken);
            var successes = samples.Count(x => x.Outcome == RaidOutcome.Slain);
            var interval = DungeonReadinessService.WilsonInterval(successes, samples.Count);
            var evaluation = new Evaluation(
                rung,
                roster.RearguardPower,
                roster.VanguardPower,
                roster.MainGuardPower,
                successes,
                samples.Count,
                interval.Lower,
                interval.Upper);
            evaluations[index] = evaluation;
            return evaluation;
        }

        var low = 0;
        var high = ladder.Count - 1;
        while (low < high)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var middle = low + (high - low) / 2;
            var evaluation = await EvaluateAsync(middle);
            if (evaluation.ClearProbability >= ClearTarget)
                high = middle;
            else
                low = middle + 1;
        }

        var selectedIndex = low;
        var selected = await EvaluateAsync(selectedIndex);
        if (selected.ClearProbability < ClearTarget)
        {
            throw new InvalidOperationException(
                $"No canonical equipment rung reached the {ClearTarget:P0} clear target for raid boss '{raidBossId}' tier {tier}.");
        }
        while (selectedIndex > 0)
        {
            var previous = await EvaluateAsync(selectedIndex - 1);
            if (previous.ClearProbability < ClearTarget)
                break;
            selected = previous;
            selectedIndex--;
        }

        var lower = await EvaluateAsync(Math.Max(0, selectedIndex - 1));
        var upper = await EvaluateAsync(Math.Min(ladder.Count - 1, selectedIndex + 1));
        var width = selected.UpperBound - selected.LowerBound;
        var confidence = width <= 0.15m
            ? PowerRatingConfidence.High
            : width <= 0.30m
                ? PowerRatingConfidence.Medium
                : PowerRatingConfidence.Low;

        return new RaidPowerRecommendation(
            raidBossId,
            tier,
            Wing(selected.RearguardPower, lower.RearguardPower, upper.RearguardPower),
            Wing(selected.VanguardPower, lower.VanguardPower, upper.VanguardPower),
            Wing(selected.MainGuardPower, lower.MainGuardPower, upper.MainGuardPower),
            selected.ClearProbability,
            selected.LowerBound,
            selected.UpperBound,
            confidence,
            selected.SampleCount,
            selected.Rung.Id,
            timeProvider.GetUtcNow());
    }

    private RaidBossTierDefinition GetTier(string raidBossId, int tier)
    {
        if (tier != 0)
            throw new InvalidOperationException("Only Regular raid difficulty is calibrated.");
        var boss = definitions.Get(raidBossId)
            ?? throw new InvalidOperationException($"Raid boss '{raidBossId}' was not found.");
        return RaidPlusDifficulty.Create(boss, 0);
    }

    private Roster CreateRoster(
        RaidBossDefinition boss,
        RaidBossTierDefinition tier,
        CanonicalEquipmentProgressionRung rung)
    {
        var runId = StableRandom.Guid(
            "raid-power-calibration-run-v1",
            boss.Id,
            tier.Tier.ToString(System.Globalization.CultureInfo.InvariantCulture),
            rung.Id);
        var run = new RaidRun
        {
            Id = runId,
            RaidBossId = boss.Id,
            Tier = tier.Tier,
            DefinitionHash = GetIdentity(boss.Id, tier.Tier).DefinitionHash,
            DefinitionSnapshotJson = JsonSerializer.Serialize(tier, jsonOptions),
            LeaderCharacterId = Guid.Empty,
            CreatedAt = DateTimeOffset.UnixEpoch,
            SignupClosesAt = DateTimeOffset.UnixEpoch.AddDays(1)
        };
        var powers = new Dictionary<RaidLane, List<int>>
        {
            [RaidLane.Rearguard] = [],
            [RaidLane.Vanguard] = [],
            [RaidLane.MainGuard] = []
        };
        foreach (var lane in RaidParties.All)
        {
            for (var slot = 0; slot < tier.LaneSlots; slot++)
            {
                var role = CanonicalCooperativeRosterCatalog.ResolveRaidRole(
                    lane,
                    slot,
                    tier.LaneSlots);
                var build = canonicalBuilds.CreateBuildForArea(
                    role,
                    rung,
                    boss.LevelRequirement,
                    CanonicalEquipmentBuildFactory.GetEssenceCountForDungeonTier(tier.Tier));
                var characterId = StableRandom.Guid(
                    "raid-power-calibration-character-v1",
                    runId.ToString("N"),
                    lane.ToString(),
                    slot.ToString(System.Globalization.CultureInfo.InvariantCulture));
                var snapshot = CreateSnapshot(build, characterId, lane, slot);
                var power = CombatRatingDisplay.FromRaw(build.Rating.Overall);
                powers[lane].Add(power);
                run.Signups.Add(new RaidSignup
                {
                    RaidRun = run,
                    RaidRunId = run.Id,
                    CharacterId = characterId,
                    AccountId = characterId,
                    CharacterName = $"Calibration {lane} {slot + 1}",
                    CharacterSnapshotId = snapshot.Id,
                    CharacterSnapshot = snapshot,
                    LoadoutHash = rung.Id,
                    PowerRating = power,
                    Lane = lane,
                    WingSlotIndex = slot,
                    SignedUpAt = DateTimeOffset.UnixEpoch
                });
            }
        }

        return new Roster(
            run,
            Average(powers[RaidLane.Rearguard]),
            Average(powers[RaidLane.Vanguard]),
            Average(powers[RaidLane.MainGuard]));
    }

    private static CharacterSnapshot CreateSnapshot(
        CanonicalEquipmentBuild build,
        Guid characterId,
        RaidLane lane,
        int slot)
    {
        var snapshotId = StableRandom.Guid(
            "raid-power-calibration-snapshot-v1",
            characterId.ToString("N"),
            lane.ToString(),
            slot.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return new CharacterSnapshot
        {
            Id = snapshotId,
            CharacterId = characterId,
            Name = $"Canonical {build.Profile}",
            Level = build.Character.Level,
            BaseAttributes = build.Character.BaseAttributes.Select(x => new EntityAttributeSnapshot
            {
                CharacterSnapshotId = snapshotId,
                AttributeType = x.AttributeType,
                Value = x.Value
            }).ToList(),
            Equipment = build.Equipment.Select(x => EquipmentSnapshot.From(ToSlot(x.EquipmentBase.EquipmentType), x)).ToList(),
            EquippedEssences = build.EquippedEssences.Select((x, index) =>
                EquippedEssenceSnapshot.From(snapshotId, index, x)).ToList()
        };
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
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported equipment slot.")
    };

    private static int Average(IReadOnlyCollection<int> values) =>
        (int)Math.Round(values.Average(), MidpointRounding.AwayFromZero);

    private static RaidWingPowerRecommendation Wing(int value, int lower, int upper) =>
        new(value, Math.Min(value, lower), Math.Max(value, upper));

    private sealed record Roster(RaidRun Run, int RearguardPower, int VanguardPower, int MainGuardPower);

    private sealed record Evaluation(
        CanonicalEquipmentProgressionRung Rung,
        int RearguardPower,
        int VanguardPower,
        int MainGuardPower,
        int Successes,
        int SampleCount,
        decimal LowerBound,
        decimal UpperBound)
    {
        public decimal ClearProbability => SampleCount == 0 ? 0m : Successes / (decimal)SampleCount;
    }
}
