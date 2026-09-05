using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;

namespace Domain.Models.Items.Equipments.Progression;

public enum EquipmentUpgradeOperationKind
{
    Reinforce,
    Dismantle,
    ApplyVariant
}

public sealed record EquipmentUpgradeTierPrices(
    int Tier,
    IReadOnlyList<long> RankPartCosts,
    IReadOnlyList<long> RankCinderCosts,
    long BaseDismantleParts);

public sealed class EquipmentUpgradePrices
{
    public EquipmentUpgradePrices(
        int version,
        string partsItemBaseId,
        IReadOnlyList<EquipmentUpgradeTierPrices> tiers,
        decimal dismantleRankRecovery)
    {
        if (version < 1 || string.IsNullOrWhiteSpace(partsItemBaseId) || tiers.Count == 0
            || tiers.Select(tier => tier.Tier).Distinct().Count() != tiers.Count
            || dismantleRankRecovery < 0 || dismantleRankRecovery > 1
            || tiers.Any(tier => tier.Tier < 1
                || tier.RankPartCosts.Count != EquipmentBalance.MaximumRank
                || tier.RankCinderCosts.Count != EquipmentBalance.MaximumRank
                || tier.RankPartCosts.Any(cost => cost <= 0)
                || tier.RankCinderCosts.Any(cost => cost <= 0)
                || tier.BaseDismantleParts < 0))
            throw new ArgumentException("Invalid equipment-upgrade prices.");

        Version = version;
        PartsItemBaseId = EquipmentValidation.Id(partsItemBaseId);
        Tiers = Array.AsReadOnly(tiers.Select(tier => tier with
        {
            RankPartCosts = Array.AsReadOnly(tier.RankPartCosts.ToArray()),
            RankCinderCosts = Array.AsReadOnly(tier.RankCinderCosts.ToArray())
        }).ToArray());
        DismantleRankRecovery = dismantleRankRecovery;
    }

    public int Version { get; }
    public string PartsItemBaseId { get; }
    public IReadOnlyList<EquipmentUpgradeTierPrices> Tiers { get; }
    public decimal DismantleRankRecovery { get; }

    public EquipmentUpgradeTierPrices ForTier(int tier) =>
        Tiers.SingleOrDefault(candidate => candidate.Tier == tier)
        ?? throw new InvalidOperationException("Upgrade prices are not authored for this equipment tier.");

    public long GetDismantleParts(int tier, int rank)
    {
        if (rank < 0 || rank > EquipmentBalance.MaximumRank)
            throw new ArgumentOutOfRangeException(nameof(rank));

        var prices = ForTier(tier);
        var awardedAndPurchasedRankValue = prices.RankPartCosts.Take(rank).Sum();
        return checked(prices.BaseDismantleParts
            + (long)decimal.Floor(awardedAndPurchasedRankValue * DismantleRankRecovery));
    }
}

public sealed record EquipmentUpgradeRequest(
    EquipmentUpgradeOperationKind Kind,
    Guid ItemInstanceId,
    bool AllowFavoriteDismantle = false,
    string? BlueprintStyleId = null);

public sealed record EquipmentUpgradeQuote(
    Guid OperationId,
    EquipmentUpgradeRequest Request,
    string Token,
    DateTimeOffset ExpiresAtUtc,
    bool CanExecute,
    string? UnavailableReason,
    EquipmentData? Before,
    EquipmentData? After,
    long PartsCost,
    long CinderCost,
    long PartsReturned,
    long AvailableParts,
    long AvailableCinders,
    uint ItemVersion,
    int PriceVersion,
    string? BlueprintItemId = null,
    long AvailableBlueprints = 0);

public sealed record EquipmentUpgradeOutcome(
    Guid OperationId,
    EquipmentUpgradeOperationKind Kind,
    Guid ItemInstanceId,
    EquipmentData? Before,
    EquipmentData? After,
    long PartsSpent,
    long CindersSpent,
    long PartsReturned,
    DateTimeOffset OccurredAtUtc,
    string? BlueprintItemId = null);

public sealed class EquipmentUpgradeReceipt
{
    public Guid CharacterId { get; init; }
    public Guid OperationId { get; init; }
    public string RequestFingerprint { get; init; } = string.Empty;
    public EquipmentUpgradeOutcome Outcome { get; init; } = null!;
}

public sealed record EquipmentUpgradeResult(
    EquipmentUpgradeOutcome? Outcome,
    string? Error,
    EquipmentUpgradeQuote? FreshQuote = null);

public sealed record EquipmentUpgradeContext(
    Character Character,
    InventoryItem? InventoryItem,
    EquipmentInstance? Equipment,
    bool IsEquipped,
    string? UnavailableReason,
    IReadOnlyList<InventoryItem> PartStacks,
    IReadOnlyList<InventoryItem>? BlueprintStacks = null);

public interface IEquipmentUpgradeRepository
{
    Task<EquipmentUpgradeReceipt?> GetReceiptAsync(
        Guid characterId,
        Guid operationId,
        CancellationToken cancellationToken);

    Task<EquipmentUpgradeContext?> LoadAsync(
        Guid characterId,
        Guid itemId,
        bool forMutation,
        CancellationToken cancellationToken);

    Task ApplyAsync(
        EquipmentUpgradeContext context,
        EquipmentUpgradeQuote quote,
        EquipmentUpgradeReceipt receipt,
        CancellationToken cancellationToken);
}
