using Domain.Models.Dungeons.Runs;
using Domain.Models.Inventories;

namespace Domain.Models.Items.Equipments.Progression;

public sealed record EquipmentProtectionPool(string Id, string DungeonId, string FamilyId, int Difficulty,
    int EquipmentTier, int MinimumLevel, string? RequiredQuestId, double MatchingChance, int GuaranteeCompletions,
    IReadOnlyList<string> TargetDefinitionIds, int Region);

public sealed class EquipmentAcquisitionCatalog
{
    public EquipmentAcquisitionCatalog(EquipmentEvaluator evaluator, IEnumerable<EquipmentProtectionPool> pools)
    {
        Evaluator = evaluator;
        Pools = Array.AsReadOnly(pools.Select(x => x with { TargetDefinitionIds = Array.AsReadOnly(x.TargetDefinitionIds.ToArray()) }).ToArray());
        if (Pools.Select(x => x.Id).Distinct().Count() != Pools.Count || Pools.Select(x => x.DungeonId).Distinct().Count() != Pools.Count)
            throw new ArgumentException("Protection pools must have unique pool and dungeon IDs.");
        foreach (var pool in Pools)
        {
            EquipmentValidation.Id(pool.Id);
            EquipmentValidation.Id(pool.DungeonId);
            EquipmentValidation.Id(pool.FamilyId);
            if (pool.RequiredQuestId != null) EquipmentValidation.Id(pool.RequiredQuestId);
            if (pool.Difficulty is < 1 or > 3 || pool.Region < 1 || pool.MinimumLevel < 1
                || !double.IsFinite(pool.MatchingChance) || pool.MatchingChance is < 0 or > 1
                || pool.GuaranteeCompletions < 1 || pool.TargetDefinitionIds.Count == 0
                || pool.TargetDefinitionIds.Distinct().Count() != pool.TargetDefinitionIds.Count)
                throw new ArgumentException("Invalid equipment protection terms.");
            foreach (var target in pool.TargetDefinitionIds)
            {
                var definition = evaluator.GetDefinition(target);
                if (definition.Rarity != EquipmentRarity.Rare || definition.NativeStyleId is null)
                    throw new ArgumentException("Named dungeon targets must have Rare identity and an authored native style.");
                evaluator.Evaluate(target, pool.EquipmentTier, 1, definition.NativeStyleId);
            }
        }
    }
    public EquipmentEvaluator Evaluator { get; }
    public IReadOnlyList<EquipmentProtectionPool> Pools { get; }
    public EquipmentProtectionPool? FindDungeon(string id) => Pools.SingleOrDefault(x => x.DungeonId == id);
}

/// <summary>Frozen at run commitment. Completion never consults the current target or evaluator.</summary>
public sealed record DungeonEquipmentCommitment(Guid CharacterId, Guid RunId, string PoolId, string DungeonId,
    int Difficulty, double MatchingChance, int GuaranteeCompletions, EquipmentData? Target);

public sealed class EquipmentProtectionProgress
{
    public Guid CharacterId { get; init; }
    public string PoolId { get; init; } = string.Empty;
    public string? SelectedDefinitionId { get; private set; }
    public int CompletionsWithoutMatch { get; private set; }
    public long Revision { get; private set; }
    public void Select(string? definitionId)
    {
        if (SelectedDefinitionId == definitionId) return;
        SelectedDefinitionId = definitionId;
        Revision++;
    }
    public EquipmentProtectionOutcome Complete(DungeonEquipmentCommitment commitment, bool firstCompletion, double roll, DateTimeOffset now)
    {
        if (commitment.CharacterId != CharacterId || commitment.PoolId != PoolId || commitment.RunId == Guid.Empty
            || commitment.GuaranteeCompletions < 1 || !double.IsFinite(roll) || roll is < 0 or >= 1)
            throw new InvalidOperationException("Invalid protected completion.");
        var before = CompletionsWithoutMatch;
        EquipmentData? award = null;
        if (commitment.Target is { } target)
        {
            var guaranteed = firstCompletion && commitment.Difficulty == 1
                || CompletionsWithoutMatch + 1 >= commitment.GuaranteeCompletions;
            if (guaranteed || roll < commitment.MatchingChance)
            {
                var state = target.State with
                {
                    Ownership = new(guaranteed ? EquipmentOwnershipKind.BoundPersonal : EquipmentOwnershipKind.UnboundPersonal, CharacterId),
                    Provenance = new(guaranteed ? EquipmentAwardKind.ProtectedReward : EquipmentAwardKind.RandomDiscovery,
                        commitment.DungeonId, commitment.RunId.ToString("N"))
                };
                award = new(state, target.ItemBaseId, target.DisplayName, target.Rarity, target.EquipmentType,
                    target.Behavior, target.Stats, target.EquipmentSetId);
                CompletionsWithoutMatch = 0;
            }
            else CompletionsWithoutMatch = checked(CompletionsWithoutMatch + 1);
            Revision++;
        }
        return new(commitment.RunId, commitment.PoolId, before, CompletionsWithoutMatch, award, now);
    }
}

public sealed record EquipmentProtectionOutcome(Guid RunId, string PoolId, int PreviousProgress, int Progress,
    EquipmentData? Equipment, DateTimeOffset SecuredAtUtc);

/// <summary>No run/item FK: completion evidence survives claimed run deletion and salvage.</summary>
public sealed class EquipmentProtectionReceipt
{
    public Guid CharacterId { get; init; }
    public Guid RunId { get; init; }
    public EquipmentProtectionOutcome Outcome { get; init; } = null!;
    public DateTimeOffset? ClaimedAtUtc { get; set; }
}

public sealed record BaselineEquipmentRecovery(Guid OperationId, StarterEquipmentGrantKind Kind,
    IReadOnlyList<EquipmentData> Equipment, DateTimeOffset RecoveredAtUtc);
public sealed class BaselineEquipmentRecoveryReceipt
{
    public Guid CharacterId { get; init; }
    public Guid OperationId { get; init; }
    public BaselineEquipmentRecovery Outcome { get; init; } = null!;
}
public sealed record BaselineEquipmentRecoveryResult(BaselineEquipmentRecovery? Recovery, string? Error);
public sealed record BaselineEquipmentRecoveryOption(StarterEquipmentGrantKind Kind, string DefinitionId, string Name, int Entitled, int Owned, int Missing);
public sealed record EquipmentProtectionPoolView(EquipmentProtectionPool Pool, string? SelectedDefinitionId, int Progress,
    bool CanSelect, IReadOnlyList<string> MissingRequirements, IReadOnlyList<EquipmentData> Targets,
    bool FirstClearGuaranteeAvailable);
public sealed record EquipmentProgressionTargetSelectionResult(EquipmentProtectionPoolView? Pool, string? Error);

public static class BaselineEquipmentRecoveryPolicy
{
    public static IReadOnlyList<BaselineEquipmentRecoveryOption> Options(StarterEquipmentGrant grant, IEnumerable<EquipmentData> owned)
    {
        var counts = owned.Where(x => x.State.Ownership.OwnerId == grant.CharacterId
            && x.State.Ownership.Kind != EquipmentOwnershipKind.GuildOwned)
            .DistinctBy(x => x.State.Id).GroupBy(x => (x.State.DefinitionId, x.State.Tier)).ToDictionary(x => x.Key, x => x.Count());
        return grant.Equipment.GroupBy(x => (x.State.DefinitionId, x.State.Tier)).Select(group =>
        {
            var count = counts.GetValueOrDefault(group.Key);
            return new BaselineEquipmentRecoveryOption(grant.Kind, group.Key.DefinitionId, group.First().DisplayName,
                group.Count(), count, Math.Max(0, group.Count() - count));
        }).ToArray();
    }
    public static BaselineEquipmentRecovery Recover(StarterEquipmentGrant grant, IEnumerable<EquipmentData> owned, Guid operationId, DateTimeOffset now)
    {
        if (operationId == Guid.Empty) throw new ArgumentException("A recovery operation ID is required.");
        var equipment = Options(grant, owned).SelectMany(option => Enumerable.Range(0, option.Missing).Select(index =>
        {
            var original = grant.Equipment.First(x => x.State.DefinitionId == option.DefinitionId);
            var state = original.State with { Id = Guid.NewGuid(), Provenance = new(EquipmentAwardKind.Recovery,
                original.State.Provenance.SourceId, $"{operationId:N}:{option.DefinitionId}:{index}") };
            return new EquipmentData(state, original.ItemBaseId, original.DisplayName, original.Rarity,
                original.EquipmentType, original.Behavior, original.Stats, original.EquipmentSetId);
        })).ToArray();
        return new(operationId, grant.Kind, Array.AsReadOnly(equipment), now);
    }
}

public interface IEquipmentAcquisitionRepository
{
    Task<int?> GetLevelAsync(Guid characterId, CancellationToken ct);
    Task LockAsync(Guid characterId, CancellationToken ct);
    Task<EquipmentProtectionProgress?> GetProgressAsync(Guid characterId, string poolId, CancellationToken ct);
    void AddProgress(EquipmentProtectionProgress progress);
    Task<EquipmentProtectionReceipt?> GetCompletionAsync(Guid characterId, Guid runId, CancellationToken ct);
    void AddCompletion(EquipmentProtectionReceipt receipt);
    Task<BaselineEquipmentRecoveryReceipt?> GetRecoveryAsync(Guid characterId, Guid operationId, CancellationToken ct);
    void AddRecovery(BaselineEquipmentRecoveryReceipt receipt);
    Task<IReadOnlyList<EquipmentData>> GetOwnedAndPendingAsync(Guid characterId, CancellationToken ct);
    Task<IReadOnlyList<InventoryItem>> AwardRecoveryAsync(Guid characterId, BaselineEquipmentRecovery recovery, CancellationToken ct);
}
