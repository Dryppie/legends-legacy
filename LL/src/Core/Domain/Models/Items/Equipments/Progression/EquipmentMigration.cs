using System.Security.Cryptography;
using System.Text;
using Domain.Models.Attributes;

namespace Domain.Models.Items.Equipments.Progression;

public enum EquipmentMigrationLocation { Instance, PendingDungeonReward }
public sealed record EquipmentMigrationTarget(Guid ItemId, EquipmentMigrationLocation Location = EquipmentMigrationLocation.Instance, Guid? ContainerId = null);
public sealed record EquipmentMigrationChoice(Guid MigrationId, IReadOnlyList<EquipmentData> Options);
public sealed record EquipmentMigrationPreview(EquipmentMigrationTarget Target, string SourceHash,
    EquipmentData? Before, EquipmentData After, double OldBudget, double NewBudget,
    IReadOnlyList<string> Choices, string MappingReason, int RespecializationAllowance)
{
    public string ResultHash => EquipmentMigrationPolicy.Hash(After);
    public LegacyEquipmentSnapshot? LegacyBefore { get; init; }
    public int SourceBalanceVersion => LegacyBefore is null ? Before!.State.BalanceVersion : 0;
    public int TargetBalanceVersion => After.State.BalanceVersion;
}
public sealed record EquipmentMigrationAudit(int Page, int PageSize, int TotalInstances, int LegacyInstances,
    int UnversionedInstances, int LegacyPendingRewards, int HistoricalSnapshots,
    IReadOnlyList<EquipmentMigrationTarget> Targets)
{
    public int SourceBalanceVersion { get; init; } = 1;
    public int MatchingInstances => LegacyInstances;
    public int MatchingPendingRewards => LegacyPendingRewards;
    public int LegacyArenaDefenses { get; init; }
    public int UnreferencedUnversionedInstances { get; init; }
    public int UnversionedPendingRewards { get; init; }
    public int ReferencedUnversionedInstances => Math.Max(0, UnversionedInstances - UnreferencedUnversionedInstances);
    public bool ReferencedItemConversionComplete => LegacyInstances == 0 && ReferencedUnversionedInstances == 0
        && LegacyPendingRewards == 0 && UnversionedPendingRewards == 0;
    public int ActiveLegacyTournamentSnapshots { get; init; }
    public bool ItemConversionComplete => LegacyInstances == 0 && UnversionedInstances == 0
        && LegacyPendingRewards == 0 && UnversionedPendingRewards == 0;
    public bool CompetitiveSnapshotsReady => LegacyArenaDefenses == 0 && ActiveLegacyTournamentSnapshots == 0;
    public string ActivationRequirement => "Keep combat and item mutations paused during conversion; refresh Arena defenses and finish or cancel legacy tournaments before competitive activation. Historical replays remain version 17. A clean audit is necessary, not a balance certification.";
}

/// <summary>Immutable before/after receipt and a single, migration-specific respecialization credit.</summary>
public sealed class EquipmentMigrationReceipt
{
    public Guid OperationId { get; set; }
    public int Revision { get; set; }
    public int RespecializationAllowance { get; set; } = 1;
    public Guid ItemId { get; set; }
    public EquipmentMigrationLocation Location { get; set; }
    public Guid? ContainerId { get; set; }
    public string ActorId { get; set; } = "";
    public string BeforeJson { get; set; } = "";
    public string AfterJson { get; set; } = "";
    public string SourceHash { get; set; } = "";
    public string ResultHash { get; set; } = "";
    public string MappingReason { get; set; } = "";
    public DateTimeOffset AppliedAtUtc { get; set; }
    public DateTimeOffset? RolledBackAtUtc { get; set; }
    public DateTimeOffset? ChoiceUsedAtUtc { get; set; }
    public Guid? ChoiceOperationId { get; set; }
    public string? ChosenDefinitionId { get; set; }
    public string? ChoiceResultJson { get; set; }
}

public static class EquipmentMigrationPolicy
{
    public static string Hash(EquipmentData data) => Convert.ToHexString(SHA256.HashData(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
    {
        data.State, data.ItemBaseId, data.DisplayName, data.Rarity, data.EquipmentType, data.Behavior,
        Stats = data.Stats.OrderBy(x => x.Key).Select(x => new { Attribute = x.Key, x.Value }).ToArray(),
        BaseStats = data.BaseStats?.OrderBy(x => x.Key).Select(x => new { Attribute = x.Key, x.Value }).ToArray(),
        data.EquipmentSetId, data.Allocation
    })));

    public static EquipmentMigrationPreview Preview(EquipmentMigrationTarget target, EquipmentData before,
        EquipmentEvaluator current, string? chosenDefinition = null, EquipmentBalance? sourceBalance = null)
    {
        if (before.State.Id != target.ItemId || before.State.BalanceVersion >= current.Balance.Version
            || current.Balance.AttributeVersion != AttributeRules.CurrentVersion)
            throw new InvalidOperationException("Conversion requires a newer equipment release using combat version 18. Use receipt rollback to restore older equipment.");
        var legacy = before.StatVersion == AttributeRules.LegacyVersion;
        if (sourceBalance is null && before.State.BalanceVersion > 2)
            throw new InvalidOperationException("The recorded source release is required to price this equipment.");
        sourceBalance ??= new EquipmentBalance(before.State.BalanceVersion);
        if (sourceBalance.Version != before.State.BalanceVersion)
            throw new InvalidOperationException("The source balance must match the item's recorded release.");
        // Blueprint drops can add a native style to an otherwise plain definition.
        // Keep the item's frozen native/active styles; evaluation validates the active style.
        var choices = CompatibleDefinitions(before, current);
        if (choices.Length == 0) throw new InvalidOperationException("No compatible authored conversion exists for this item.");
        // Compare historical budget shares, not old effective percentage values. Two resistance
        // channels pool their spend; nonlinear cooldown equivalence is audited at loadout level.
        var intent = before.Stats.GroupBy(x => MapIntent(x.Key)).ToDictionary(g => g.Key,
            g => g.Sum(x => x.Value * sourceBalance.GetMaterializedCostPerPoint(x.Key, before.State.Tier)));
        var sum = intent.Values.Sum();
        if (!legacy)
        {
            if (chosenDefinition is not null && chosenDefinition != before.State.DefinitionId)
                throw new InvalidOperationException("A rebalance preserves the item's chosen specialization.");
            chosenDefinition = before.State.DefinitionId;
        }
        var definition = chosenDefinition is null ? choices.OrderBy(x => Distance(x)).ThenBy(x => x.Id, StringComparer.Ordinal).First()
            : choices.SingleOrDefault(x => x.Id == chosenDefinition) ?? throw new InvalidOperationException("The chosen specialization is not compatible with this item.");
        var state = EquipmentState.Restore(before.State with
        {
            ModelVersion = EquipmentBalance.ModelVersion, BalanceVersion = current.Balance.Version,
            DefinitionId = definition.Id, ArchetypeId = definition.ArchetypeId
        });
        var evaluated = EquipmentData.Create(state, current);
        var after = new EquipmentData(state.ToSnapshot(), before.ItemBaseId, before.DisplayName, before.Rarity,
            before.EquipmentType, before.Behavior, evaluated.Stats, evaluated.EquipmentSetId, evaluated.BaseStats, evaluated.Allocation);
        // Modern descriptors already record nominal spend, including reserved set identity.
        // Repricing only their visible stats would misreport identity and rounding as a budget change.
        var oldBudget = before.Allocation?.Total
            ?? before.Stats.Sum(x => x.Value * sourceBalance.GetMaterializedCostPerPoint(x.Key, before.State.Tier));
        return new(target, Hash(before), before, after, oldBudget, after.Allocation!.Total,
            legacy ? choices.Select(x => x.Id).ToArray() : [definition.Id],
            legacy ? "Preserved identity, tier, rarity, quality, rank, roll, ownership and style; selected the nearest legal profile by historical budget share. Removed affixes buy the new profile; whole-loadout cooldown and combat outcomes can change."
                : "Repriced equipment using the target release; preserved specialization, identity, tier, rarity, quality, rank, roll, ownership and style.", legacy ? 1 : 0);

        double Distance(EquipmentDefinition option)
        {
            var evaluation = current.Evaluate(option.Id, before.State.Tier, before.State.Rank, before.State.ActiveStyleId,
                before.Quality, before.AttributeRollMultiplier, before.State.AdditiveVariantBonus);
            var spend = evaluation.Stats.ToDictionary(x => x.Key, x => x.Value * current.Balance.GetMaterializedCostPerPoint(x.Key, before.State.Tier));
            var total = spend.Values.Sum();
            return intent.Keys.Union(spend.Keys).Sum(key => Math.Abs(intent.GetValueOrDefault(key) / sum - spend.GetValueOrDefault(key) / total));
        }
    }

    public static EquipmentDefinition[] CompatibleDefinitions(EquipmentData item, EquipmentEvaluator evaluator) =>
        evaluator.Definitions.Where(x => x.ArchetypeId == (item.StatVersion == AttributeRules.LegacyVersion
                ? LegacyEquipmentMigration.ResolveArchetype(item.State.ArchetypeId) : item.State.ArchetypeId)
            && evaluator.GetArchetype(x.ArchetypeId).EquipmentType == item.EquipmentType && x.Rarity == item.Rarity
            && (x.NativeStyleId is null || x.NativeStyleId == item.State.NativeStyleId))
            .OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();

    private static AttributeType MapIntent(AttributeType attribute) => attribute switch
    {
        AttributeType.Cooldown => AttributeType.AbilityHaste,
        AttributeType.StatusResistance or AttributeType.CrowdControlResistance => AttributeType.Tenacity,
        AttributeType.HealingPowerPercent => AttributeType.Restoration,
        AttributeType.LifeSteal => AttributeType.HealthRegeneration,
        AttributeType.DamageReduction or AttributeType.DodgeChance => AttributeType.MaxHealth,
        _ => attribute
    };
}

public interface IEquipmentMigrationRepository
{
    Task AssertNoScheduledCombatAsync(IReadOnlyList<Guid> characterIds, CancellationToken ct);
    Task<EquipmentMigrationAudit> AuditAsync(int page, int pageSize, CancellationToken ct, int sourceBalanceVersion = 1);
    Task<EquipmentData?> LoadAsync(EquipmentMigrationTarget target, bool forMutation, CancellationToken ct);
    Task<LegacyEquipmentSnapshot?> LoadLegacyAsync(EquipmentMigrationTarget target, CancellationToken ct) =>
        Task.FromResult<LegacyEquipmentSnapshot?>(null);
    Task RestoreLegacyAsync(EquipmentMigrationTarget target, LegacyEquipmentSnapshot original,
        EquipmentMigrationReceipt receipt, CancellationToken ct) =>
        throw new InvalidOperationException("Legacy equipment restoration is not supported by this repository.");
    Task<IReadOnlyList<Guid>> GetAffectedCharactersAsync(EquipmentMigrationTarget target, CancellationToken ct);
    Task<EquipmentMigrationReceipt?> GetReceiptAsync(Guid operationId, CancellationToken ct);
    Task<EquipmentMigrationReceipt?> GetActiveReceiptForItemAsync(Guid itemId, CancellationToken ct);
    Task SaveAsync(EquipmentMigrationTarget target, EquipmentData data, EquipmentMigrationReceipt receipt, bool isNew, CancellationToken ct);
}
