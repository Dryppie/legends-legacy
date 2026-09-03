namespace Domain.Models.Items.Equipments.Progression;

public enum EquipmentAwardKind
{
    RandomDiscovery = 0,
    ProtectedReward = 1,
    QuestReward = 2,
    Recovery = 3,
    Administrative = 5
}

public enum EquipmentOwnershipKind
{
    UnboundPersonal = 0,
    BoundPersonal = 1,
    GuildOwned = 2
}

public sealed record EquipmentProvenance
{
    public EquipmentProvenance(EquipmentAwardKind kind, string sourceId, string awardId)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        Kind = kind;
        SourceId = EquipmentValidation.Id(sourceId);
        AwardId = EquipmentValidation.Id(awardId);
    }

    public EquipmentAwardKind Kind { get; }
    public string SourceId { get; }
    public string AwardId { get; }
}

public sealed record EquipmentOwnership
{
    public EquipmentOwnership(EquipmentOwnershipKind kind, Guid ownerId)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Equipment must have an owner.", nameof(ownerId));
        Kind = kind;
        OwnerId = ownerId;
    }

    public EquipmentOwnershipKind Kind { get; }
    /// <summary>Character ID for personal equipment; guild ID for guild property.</summary>
    public Guid OwnerId { get; }
    public bool CanTradeOrDonate => Kind == EquipmentOwnershipKind.UnboundPersonal;
}

/// <summary>
/// Immutable domain state carried by the persisted Equipment progression descriptor.
/// Methods return proposed state;
/// the application must atomically commit it with payment/award receipts and
/// enforce inventory availability, ownership authorization and combat locks.
/// </summary>
public sealed class EquipmentState
{
    private EquipmentState(
        Guid id, string definitionId, string archetypeId, int tier, int rank, int balanceVersion,
        string? nativeStyleId, string? activeStyleId, EquipmentProvenance provenance,
        EquipmentOwnership ownership)
    {
        Id = id;
        DefinitionId = definitionId;
        ArchetypeId = archetypeId;
        Tier = tier;
        Rank = rank;
        BalanceVersion = balanceVersion;
        NativeStyleId = nativeStyleId;
        ActiveStyleId = activeStyleId;
        Provenance = provenance;
        Ownership = ownership;
    }

    public int ModelVersion => EquipmentBalance.ModelVersion;
    public Guid Id { get; }
    public string DefinitionId { get; }
    public string ArchetypeId { get; }
    public int Tier { get; }
    public int Rank { get; }
    public int BalanceVersion { get; }
    public string? NativeStyleId { get; }
    public string? ActiveStyleId { get; }
    public EquipmentProvenance Provenance { get; }
    public EquipmentOwnership Ownership { get; }
    public EquipmentStateSnapshot ToSnapshot() => new(
        ModelVersion, Id, DefinitionId, ArchetypeId, Tier, Rank, BalanceVersion,
        NativeStyleId, ActiveStyleId, Provenance, Ownership);

    public static EquipmentState Restore(EquipmentStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.ModelVersion != EquipmentBalance.ModelVersion)
            throw new InvalidOperationException("Unsupported Equipment progression state version.");
        if (snapshot.Id == Guid.Empty || snapshot.Tier < 1 || snapshot.Tier > 100
            || snapshot.Rank < 0 || snapshot.Rank > EquipmentBalance.MaximumRank
            || snapshot.BalanceVersion < 1)
            throw new InvalidOperationException("Invalid persisted Equipment progression equipment state.");
        EquipmentValidation.Id(snapshot.DefinitionId);
        EquipmentValidation.Id(snapshot.ArchetypeId);
        if (snapshot.NativeStyleId is not null) EquipmentValidation.Id(snapshot.NativeStyleId);
        if (snapshot.ActiveStyleId is not null) EquipmentValidation.Id(snapshot.ActiveStyleId);
        ArgumentNullException.ThrowIfNull(snapshot.Provenance);
        ArgumentNullException.ThrowIfNull(snapshot.Ownership);
        if (snapshot.Provenance.Kind != EquipmentAwardKind.RandomDiscovery
            && snapshot.Ownership.Kind == EquipmentOwnershipKind.UnboundPersonal)
            throw new InvalidOperationException("A guaranteed award cannot regain unbound ownership.");
        var result = new EquipmentState(snapshot.Id, snapshot.DefinitionId, snapshot.ArchetypeId,
            snapshot.Tier, snapshot.Rank, snapshot.BalanceVersion, snapshot.NativeStyleId, snapshot.ActiveStyleId,
            snapshot.Provenance, snapshot.Ownership);
        return result;
    }

    public static EquipmentState Award(
        Guid id, EquipmentEvaluator evaluator, string definitionId, int tier, int rank,
        EquipmentProvenance provenance, EquipmentOwnership ownership)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Equipment must have a stable instance ID.", nameof(id));
        ArgumentNullException.ThrowIfNull(evaluator);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(ownership);
        var definition = evaluator.GetDefinition(definitionId);
        evaluator.Evaluate(definitionId, tier, rank, definition.NativeStyleId);
        if (provenance.Kind != EquipmentAwardKind.RandomDiscovery && ownership.Kind == EquipmentOwnershipKind.UnboundPersonal)
            ownership = new EquipmentOwnership(EquipmentOwnershipKind.BoundPersonal, ownership.OwnerId);
        return new EquipmentState(
            id, definitionId, definition.ArchetypeId, tier, rank, evaluator.Balance.Version,
            definition.NativeStyleId, definition.NativeStyleId, provenance, ownership);
    }

    public EquipmentState BindForPersonalUse()
    {
        RequirePersonalOwnership();
        return Ownership.Kind == EquipmentOwnershipKind.BoundPersonal ? this
            : Copy(new EquipmentOwnership(EquipmentOwnershipKind.BoundPersonal, Ownership.OwnerId));
    }

    public EquipmentState DonateToGuild(Guid guildId)
    {
        if (!Ownership.CanTradeOrDonate)
            throw new InvalidOperationException("Only an unbound discovery can be donated.");
        return Copy(new EquipmentOwnership(EquipmentOwnershipKind.GuildOwned, guildId));
    }

    public EquipmentState TransferToCharacter(Guid expectedOwnerId, Guid recipientId)
    {
        if (Ownership.OwnerId != expectedOwnerId || !Ownership.CanTradeOrDonate)
            throw new InvalidOperationException("Only the owner's unbound discovery can be transferred.");
        return Copy(new(EquipmentOwnershipKind.UnboundPersonal, recipientId));
    }

    private void RequirePersonalOwnership()
    {
        if (Ownership.Kind == EquipmentOwnershipKind.GuildOwned)
            throw new InvalidOperationException("Guild equipment must retain guild ownership.");
    }

    private EquipmentState Copy(EquipmentOwnership ownership) =>
        new(Id, DefinitionId, ArchetypeId, Tier, Rank, BalanceVersion, NativeStyleId, ActiveStyleId,
            Provenance, ownership);
}

public sealed record EquipmentStateSnapshot(
    int ModelVersion, Guid Id, string DefinitionId, string ArchetypeId, int Tier, int Rank, int BalanceVersion,
    string? NativeStyleId, string? ActiveStyleId, EquipmentProvenance Provenance,
    EquipmentOwnership Ownership);
