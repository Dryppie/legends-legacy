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
    public bool CanPersonallyModifyOrSalvage => Kind != EquipmentOwnershipKind.GuildOwned;
}

public sealed record EquipmentRankInvestment(Guid OperationId, int Rank, long Scrap, long Cinders);

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
        EquipmentOwnership ownership, long baseSalvageScrap,
        IEnumerable<EquipmentRankInvestment> investments)
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
        BaseSalvageScrap = baseSalvageScrap;
        Investments = Array.AsReadOnly(investments.ToArray());
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
    public long BaseSalvageScrap { get; }
    public IReadOnlyList<EquipmentRankInvestment> Investments { get; }
    public long PaidScrap => Investments.Sum(x => x.Scrap);
    public long PaidCinders => Investments.Sum(x => x.Cinders);

    public EquipmentStateSnapshot ToSnapshot() => new(
        ModelVersion, Id, DefinitionId, ArchetypeId, Tier, Rank, BalanceVersion,
        NativeStyleId, ActiveStyleId, Provenance, Ownership, BaseSalvageScrap, Investments);

    public static EquipmentState Restore(EquipmentStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.ModelVersion != EquipmentBalance.ModelVersion)
            throw new InvalidOperationException("Unsupported Equipment progression state version.");
        if (snapshot.Id == Guid.Empty || snapshot.Tier < 1 || snapshot.Tier > 100
            || snapshot.Rank < 0 || snapshot.Rank > EquipmentBalance.MaximumRank
            || snapshot.BalanceVersion < 1 || snapshot.BaseSalvageScrap < 0)
            throw new InvalidOperationException("Invalid persisted Equipment progression equipment state.");
        EquipmentValidation.Id(snapshot.DefinitionId);
        EquipmentValidation.Id(snapshot.ArchetypeId);
        if (snapshot.NativeStyleId is not null) EquipmentValidation.Id(snapshot.NativeStyleId);
        if (snapshot.ActiveStyleId is not null) EquipmentValidation.Id(snapshot.ActiveStyleId);
        ArgumentNullException.ThrowIfNull(snapshot.Provenance);
        ArgumentNullException.ThrowIfNull(snapshot.Ownership);
        ArgumentNullException.ThrowIfNull(snapshot.Investments);
        var receipts = snapshot.Investments.ToArray();
        if (receipts.Any(x => x is null || x.OperationId == Guid.Empty || x.Scrap <= 0 || x.Cinders <= 0
                || x.Rank < 1 || x.Rank > snapshot.Rank)
            || receipts.Select(x => x.OperationId).Distinct().Count() != receipts.Length
            || receipts.Where((x, i) => x.Rank != snapshot.Rank - receipts.Length + i + 1).Any())
            throw new InvalidOperationException("Invalid persisted rank investment receipts.");
        if (snapshot.Provenance.Kind != EquipmentAwardKind.RandomDiscovery
            && (snapshot.BaseSalvageScrap != 0 || snapshot.Ownership.Kind == EquipmentOwnershipKind.UnboundPersonal))
            throw new InvalidOperationException("A guaranteed award cannot regain discovery value or unbound ownership.");
        if (snapshot.Ownership.Kind == EquipmentOwnershipKind.UnboundPersonal
            && (receipts.Length > 0 || snapshot.ActiveStyleId != snapshot.NativeStyleId))
            throw new InvalidOperationException("Personally modified equipment cannot be unbound.");
        var result = new EquipmentState(snapshot.Id, snapshot.DefinitionId, snapshot.ArchetypeId,
            snapshot.Tier, snapshot.Rank, snapshot.BalanceVersion, snapshot.NativeStyleId, snapshot.ActiveStyleId,
            snapshot.Provenance, snapshot.Ownership, snapshot.BaseSalvageScrap, receipts);
        _ = result.PaidScrap;
        _ = result.PaidCinders;
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
        var isDiscovery = provenance.Kind == EquipmentAwardKind.RandomDiscovery;
        if (!isDiscovery && ownership.Kind == EquipmentOwnershipKind.UnboundPersonal)
            ownership = new EquipmentOwnership(EquipmentOwnershipKind.BoundPersonal, ownership.OwnerId);
        return new EquipmentState(
            id, definitionId, definition.ArchetypeId, tier, rank, evaluator.Balance.Version,
            definition.NativeStyleId, definition.NativeStyleId, provenance, ownership,
            isDiscovery ? definition.RandomDiscoveryBaseScrap : 0, []);
    }

    public EquipmentState BindForPersonalUse()
    {
        RequirePersonalOwnership();
        return Ownership.Kind == EquipmentOwnershipKind.BoundPersonal ? this
            : Copy(Rank, ActiveStyleId, new EquipmentOwnership(EquipmentOwnershipKind.BoundPersonal, Ownership.OwnerId), Investments);
    }

    public EquipmentState DonateToGuild(Guid guildId)
    {
        if (!Ownership.CanTradeOrDonate)
            throw new InvalidOperationException("Only an unbound discovery can be donated.");
        return Copy(Rank, ActiveStyleId, new EquipmentOwnership(EquipmentOwnershipKind.GuildOwned, guildId), Investments);
    }

    public EquipmentState TransferToCharacter(Guid expectedOwnerId, Guid recipientId)
    {
        if (Ownership.OwnerId != expectedOwnerId || !Ownership.CanTradeOrDonate)
            throw new InvalidOperationException("Only the owner's unbound discovery can be transferred.");
        return Copy(Rank, ActiveStyleId, new(EquipmentOwnershipKind.UnboundPersonal, recipientId), Investments);
    }

    public EquipmentState ChangeStyle(
        EquipmentEvaluator evaluator, string? styleId, IReadOnlySet<string> learnedStyleIds)
    {
        ArgumentNullException.ThrowIfNull(learnedStyleIds);
        evaluator.Evaluate(this); // Reject mismatched balance versions even on no-op requests.
        if (styleId == ActiveStyleId)
            return this;
        RequirePersonalOwnership();
        if (styleId is not null && styleId != NativeStyleId && !learnedStyleIds.Contains(styleId))
            throw new InvalidOperationException("The character has not learned this style.");
        evaluator.Evaluate(DefinitionId, Tier, Rank, styleId);
        return Copy(Rank, styleId, new EquipmentOwnership(EquipmentOwnershipKind.BoundPersonal, Ownership.OwnerId), Investments);
    }

    /// <summary>
    /// Records actual paid amounts, not a price inferred from rank. The future
    /// Forge application command owns price validation, debit and idempotency.
    /// This receipt also prevents a retried operation from advancing rank twice.
    /// </summary>
    public EquipmentState RecordPaidRankImprovement(
        EquipmentEvaluator evaluator, Guid operationId, long scrap, long cinders)
    {
        if (operationId == Guid.Empty || scrap <= 0 || cinders <= 0)
            throw new ArgumentException("A paid rank improvement requires an operation ID and positive actual payments.");
        var before = evaluator.Evaluate(this);
        var receipt = Investments.SingleOrDefault(x => x.OperationId == operationId);
        if (receipt is not null)
        {
            if (receipt.Scrap != scrap || receipt.Cinders != cinders)
                throw new InvalidOperationException("A rank operation cannot be replayed with different payments.");
            return this;
        }
        RequirePersonalOwnership();
        if (Rank == EquipmentBalance.MaximumRank)
            throw new InvalidOperationException("Equipment is already at its maximum rank.");
        var after = evaluator.Evaluate(DefinitionId, Tier, Rank + 1, ActiveStyleId);
        var attributes = before.Stats.Keys.Union(after.Stats.Keys).ToArray();
        if (attributes.Any(a => after.Stats.GetValueOrDefault(a) < before.Stats.GetValueOrDefault(a))
            || !attributes.Any(a => after.Stats.GetValueOrDefault(a) > before.Stats.GetValueOrDefault(a)))
            throw new InvalidOperationException("The next rank does not provide a representable improvement.");
        // Detect ledger overflow before producing the new state.
        _ = checked(PaidScrap + scrap);
        _ = checked(PaidCinders + cinders);
        return Copy(Rank + 1, ActiveStyleId,
            new EquipmentOwnership(EquipmentOwnershipKind.BoundPersonal, Ownership.OwnerId),
            Investments.Append(new EquipmentRankInvestment(operationId, Rank + 1, scrap, cinders)));
    }

    public long GetSalvageScrap(decimal paidRecoveryFraction = 0.5m)
    {
        RequirePersonalOwnership();
        if (paidRecoveryFraction < 0 || paidRecoveryFraction >= 1)
            throw new ArgumentOutOfRangeException(nameof(paidRecoveryFraction));
        return checked(BaseSalvageScrap + (long)decimal.Floor(PaidScrap * paidRecoveryFraction));
    }

    private void RequirePersonalOwnership()
    {
        if (!Ownership.CanPersonallyModifyOrSalvage)
            throw new InvalidOperationException("Guild equipment must retain guild ownership and cannot be personally modified or salvaged.");
    }

    private EquipmentState Copy(int rank, string? styleId, EquipmentOwnership ownership,
        IEnumerable<EquipmentRankInvestment> investments) =>
        new(Id, DefinitionId, ArchetypeId, Tier, rank, BalanceVersion, NativeStyleId, styleId,
            Provenance, ownership, BaseSalvageScrap, investments);
}

public sealed record EquipmentStateSnapshot(
    int ModelVersion, Guid Id, string DefinitionId, string ArchetypeId, int Tier, int Rank, int BalanceVersion,
    string? NativeStyleId, string? ActiveStyleId, EquipmentProvenance Provenance,
    EquipmentOwnership Ownership, long BaseSalvageScrap,
    IReadOnlyList<EquipmentRankInvestment> Investments);
