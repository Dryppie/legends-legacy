using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;

namespace Domain.Models.Items.Equipments.Progression;

public enum ForgeOperationKind { ImproveRank, ChangeStyle, Salvage, LearnStyle }

public sealed record EquipmentProgressionStyleSource(string Id, string Name, string ItemBaseId, EquipmentStyle Style);

public sealed record ForgeTierPrices(int Tier, IReadOnlyList<long> RankScrapCosts,
    IReadOnlyList<long> RankCinderCosts, long StyleChangeCinders);

public sealed class ForgePrices
{
    public ForgePrices(int version, IReadOnlyList<ForgeTierPrices> tiers, decimal paidScrapRecovery)
    {
        if (version < 1 || tiers.Count == 0 || tiers.Select(t => t.Tier).Distinct().Count() != tiers.Count
            || paidScrapRecovery < 0 || paidScrapRecovery >= 1
            || tiers.Any(t => t.Tier < 1 || t.RankScrapCosts.Count != EquipmentBalance.MaximumRank
                || t.RankCinderCosts.Count != EquipmentBalance.MaximumRank
                || t.RankScrapCosts.Any(x => x <= 0) || t.RankCinderCosts.Any(x => x <= 0) || t.StyleChangeCinders < 0))
            throw new ArgumentException("Invalid Equipment progression Forge prices.");
        Version = version;
        Tiers = Array.AsReadOnly(tiers.Select(t => t with {
            RankScrapCosts = Array.AsReadOnly(t.RankScrapCosts.ToArray()),
            RankCinderCosts = Array.AsReadOnly(t.RankCinderCosts.ToArray()) }).ToArray());
        PaidScrapRecovery = paidScrapRecovery;
    }
    public int Version { get; }
    public IReadOnlyList<ForgeTierPrices> Tiers { get; }
    public decimal PaidScrapRecovery { get; }
    public ForgeTierPrices ForTier(int tier) => Tiers.SingleOrDefault(t => t.Tier == tier)
        ?? throw new InvalidOperationException("Forge prices are not authored for this equipment tier.");
}

public sealed class LearnedEquipmentStyle
{
    public Guid CharacterId { get; init; }
    public string StyleId { get; init; } = string.Empty;
    public DateTimeOffset LearnedAtUtc { get; init; }
    public Guid? FreeApplicationOperationId { get; private set; }

    public void UseFreeApplication(Guid operationId)
    {
        if (operationId == Guid.Empty || FreeApplicationOperationId is not null)
            throw new InvalidOperationException("The free style application has already been used.");
        FreeApplicationOperationId = operationId;
    }
}

public sealed record ForgeRequest(ForgeOperationKind Kind, Guid ItemInstanceId, string? StyleId = null,
    bool AllowFavoriteSalvage = false);

public sealed record ForgeQuote(Guid OperationId, ForgeRequest Request, string Token, DateTimeOffset ExpiresAtUtc,
    bool CanExecute, string? UnavailableReason, EquipmentData? Before, EquipmentData? After,
    long ScrapCost, long CinderCost, long ScrapReturned, bool UsesFreeApplication, bool IsNoOp,
    uint ItemVersion, int PriceVersion)
{
    public ForgeLoadoutImpact? EquippedImpact { get; init; }
}

public sealed record ForgeOutcome(Guid OperationId, ForgeOperationKind Kind, Guid ItemInstanceId,
    string? StyleId, EquipmentData? Before, EquipmentData? After, long ScrapSpent, long CindersSpent,
    long ScrapReturned, bool UsedFreeApplication, bool WasNoOp, DateTimeOffset OccurredAtUtc);

/// <summary>Independent of the item lifetime, so salvage retries can replay the original outcome.</summary>
public sealed class ForgeReceipt
{
    public Guid CharacterId { get; init; }
    public Guid OperationId { get; init; }
    public string RequestFingerprint { get; init; } = string.Empty;
    public ForgeOutcome Outcome { get; init; } = null!;
}

public sealed record ForgeResult(ForgeOutcome? Outcome, string? Error, ForgeQuote? FreshQuote = null);

public sealed record ForgeStyleOption(string Id, string Name, bool IsLearned, bool FreeApplicationAvailable,
    bool IsCompatible, bool IsNative, bool IsActive)
{
    public string ItemBaseId { get; init; } = string.Empty;
}

public sealed record ForgeContext(Character Character, InventoryItem? InventoryItem, EquipmentInstance? Equipment,
    bool IsEquipped, string? UnavailableReason, IReadOnlyList<InventoryItem> ScrapStacks,
    IReadOnlyList<LearnedEquipmentStyle> LearnedStyles);

public interface IForgeRepository
{
    Task<ForgeReceipt?> GetReceiptAsync(Guid characterId, Guid operationId, CancellationToken cancellationToken);
    Task<ForgeContext?> LoadAsync(Guid characterId, Guid itemId, bool forMutation, CancellationToken cancellationToken);
    Task ApplyAsync(ForgeContext context, ForgeQuote quote, ForgeReceipt receipt, CancellationToken cancellationToken);
}
