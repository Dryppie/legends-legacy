using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.CombatStyles;
using Domain.Models.Items.Equipments.Progression;

namespace Domain.Models.Analytics;

/// <summary>Committed observations only. Names, chat and authentication data are deliberately absent.</summary>
public sealed record ItemizationObservation(
    string Id, string Kind, Guid CharacterId, DateTimeOffset OccurredAtUtc,
    string Context, int RulesVersion, Guid? ItemId = null, EquipmentData? Equipment = null,
    IReadOnlyList<Guid>? ReplacedItemIds = null, ItemizationBuildSnapshot? Build = null,
    ItemizationBattleSnapshot? Battle = null)
{
    public ItemizationComparison? Comparison { get; init; }
    public ItemizationChoiceContext? Choices { get; init; }
    public int SchemaVersion { get; init; } = 2;
    public double InclusionProbability { get; init; } = 1;
    public static string StableId(string kind, string operation, Guid subject) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{kind}|{operation}|{subject:N}")));
}

public sealed record ItemizationComparison(Guid RequestId, Guid CandidateId, string Slot, int CharacterLevel,
    string? Doctrine, IReadOnlyList<string> Essences, IReadOnlyList<ItemizationComparisonDelta> Attributes);
public sealed record ItemizationComparisonDelta(AttributeType Attribute, double Before, double After);

public sealed record ItemizationEssence(string Id, int Ascension, int Order);
public sealed record ItemizationStat(AttributeType Attribute, double Raw, double Effective, double UnusedAtCap, double NormalizedBudgetUnused = 0);
public sealed record ItemizationBuildSnapshot(
    string Hash, int Level, IReadOnlyList<EquipmentData> Equipment,
    IReadOnlyList<ItemizationEssence> Essences, CombatStyleSnapshot? Doctrine,
    IReadOnlyList<ItemizationStat> Attributes)
{
    public static ItemizationBuildSnapshot Capture(CombatEntity entity)
    {
        var gear = entity.Equipment.Where(x => x.ProgressionData is not null)
            .DistinctBy(x => x.Id).OrderBy(x => x.Id).Select(x => x.ProgressionData!).ToArray();
        var essences = entity.EquippedEssences.Select((x, index) => new ItemizationEssence(x.EssenceDefinitionId, x.AscensionTier, index)).ToArray();
        var attributes = entity.CombatAttributes.OrderBy(x => x.Key).Select(x =>
        {
            var effective = entity.AttributeRulesVersion == AttributeRules.CurrentVersion
                ? AttributeRules.Effective(x.Key, x.Value) : x.Value;
            if (x.Key == AttributeType.AttackSpeed)
                effective = Math.Clamp(x.Value, 0, AttributeCombatRules.CalculateUsefulAttackSpeedCapPercent(
                    entity.MainHandEquipment?.ProgressionData?.Behavior.BasicAttackIntervalMultiplier ?? 1d));
            var unused = Math.Max(0, x.Value - effective);
            var purchased = gear.Where(item => item.StatVersion == AttributeRules.CurrentVersion).Sum(item =>
                item.Stats.GetValueOrDefault(x.Key));
            var wastedBudget = EquipmentStatBudgetCatalog.IsKnown(x.Key) && EquipmentStatBudgetCatalog.IsDirectPercentage(x.Key)
                ? Math.Min(unused, purchased) * EquipmentStatBudgetCatalog.Get(x.Key).CostPerPoint : 0;
            return new ItemizationStat(x.Key, x.Value, effective, unused, wastedBudget);
        }).ToArray();
        var snapshot = new ItemizationBuildSnapshot("", entity.Level, gear, essences, entity.CombatStyle, attributes);
        var identity = new { entity.Level, RulesVersion = entity.AttributeRulesVersion,
            Gear = gear.Select(EquipmentMigrationPolicy.Hash).ToArray(), Essences = essences,
            Doctrine = entity.CombatStyle, Attributes = attributes };
        return snapshot with { Hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(identity))) };
    }
}

public sealed record ItemizationBattleSnapshot(
    Guid EncounterId, string Mode, string EnemyProfile, string Outcome, int DurationTicks,
    EntityStats Participant)
{
    public static EntityStats WithoutNames(EntityStats stats) => stats with
    {
        EntityName = "", Abilities = [],
        TargetInteractions = stats.TargetInteractions.Select(x => x with { TargetName = "" }).ToList()
    };
}

public sealed class ItemizationObservationRow
{
    public string Id { get; set; } = "";
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid CharacterId { get; set; }
    public string Kind { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
}

public sealed class ItemizationDailyReport
{
    public DateOnly Day { get; set; }
    public string PayloadJson { get; set; } = "{}";
}

public interface IItemizationTelemetryRepository
{
    Task RecordAsync(ItemizationObservation observation, CancellationToken ct);
    Task GenerateDailyReportsAsync(DateOnly yesterday, CancellationToken ct);
    Task<IReadOnlyList<ItemizationCohortReport>> GetReportsAsync(int days, CancellationToken ct);
}
