namespace Domain.Models.Items.Equipments.Progression;

/// <summary>Frozen, earned plain copies. Inventory loss never removes the entitlement.</summary>
public sealed class PlainEquipmentEntitlement
{
    public Guid CharacterId { get; init; }
    public string DefinitionId { get; init; } = string.Empty;
    public int Tier { get; init; }
    public EquipmentData Baseline { get; init; } = null!;
    public int Copies { get; private set; }
    public void RecordAward(EquipmentData award)
    {
        if (award.State.DefinitionId != DefinitionId || award.State.Tier != Tier
            || award.State.Ownership.OwnerId != CharacterId || award.State.Ownership.Kind != EquipmentOwnershipKind.BoundPersonal
            || award.State.Provenance.Kind != EquipmentAwardKind.ProtectedReward || award.State.Rank != 0
            || award.State.ActiveStyleId != null)
            throw new ArgumentException("Only earned plain target awards establish recovery rights.");
        Copies = checked(Copies + 1);
    }

    public PlainEquipmentRecoveryOption GetOption(IEnumerable<EquipmentData> owned, IEnumerable<EquipmentData> starters)
    {
        var entitled = checked(Copies + starters.Count(x => x.State.DefinitionId == DefinitionId && x.State.Tier == Tier));
        var count = owned.Where(x => x.State.DefinitionId == DefinitionId && x.State.Tier == Tier
            && x.State.Ownership.OwnerId == CharacterId && x.State.Ownership.Kind != EquipmentOwnershipKind.GuildOwned)
            .DistinctBy(x => x.State.Id).Count();
        return new(DefinitionId, Tier, Baseline.DisplayName, entitled, count, Math.Max(0, entitled - count));
    }

    public PlainEquipmentRecovery Recover(IEnumerable<EquipmentData> owned, IEnumerable<EquipmentData> starters,
        Guid operationId, DateTimeOffset now)
    {
        if (operationId == Guid.Empty) throw new ArgumentException("A recovery operation ID is required.");
        var originalStarters = starters.Where(x => x.State.DefinitionId == DefinitionId && x.State.Tier == Tier).ToArray();
        var option = GetOption(owned, originalStarters);
        var copies = originalStarters.Concat(Enumerable.Repeat(Baseline, Copies)).Skip(option.Owned)
            .Select((baseline, index) => new EquipmentData(
            baseline.State with { Id = Guid.NewGuid(), Provenance = new(EquipmentAwardKind.Recovery,
                baseline.State.Provenance.SourceId, $"{operationId:N}:{index}") },
            baseline.ItemBaseId, baseline.DisplayName, baseline.Rarity, baseline.EquipmentType,
            baseline.Behavior, baseline.Stats, baseline.EquipmentSetId)).ToArray();
        return new(operationId, DefinitionId, Tier, copies, now);
    }
}

public sealed record PlainEquipmentRecoveryOption(string DefinitionId, int Tier, string Name, int Entitled, int Owned, int Missing);
public sealed record PlainEquipmentRecovery(Guid OperationId, string DefinitionId, int Tier,
    IReadOnlyList<EquipmentData> Equipment, DateTimeOffset RecoveredAtUtc);
public sealed record PlainEquipmentRecoveryResult(PlainEquipmentRecovery? Recovery, string? Error);
public sealed class PlainEquipmentRecoveryReceipt
{
    public Guid CharacterId { get; init; }
    public Guid OperationId { get; init; }
    public PlainEquipmentRecovery Outcome { get; init; } = null!;
}

public interface IPlainEquipmentRepository
{
    Task<IReadOnlyList<PlainEquipmentEntitlement>> GetAsync(Guid characterId, CancellationToken ct);
    Task RecordAwardAsync(Guid characterId, EquipmentData award, CancellationToken ct);
    Task<PlainEquipmentRecovery?> GetRecoveryAsync(Guid characterId, Guid operationId, CancellationToken ct);
    Task AwardRecoveryAsync(Guid characterId, PlainEquipmentRecovery recovery, CancellationToken ct);
}
