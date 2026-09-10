using Domain.Models.CombatStyles;
using Domain.Models.Essences;

namespace Application.Interfaces.Services.LL.CombatStyles;

public interface ICombatStyleService
{
    Task<CombatStyleOverview> GetOverviewAsync(Guid characterId, CancellationToken ct);
    Task<CombatStyleOverview> PreviewAsync(Guid characterId, CombatStyleSelectionRequest selection, CancellationToken ct);
    /// <summary>
    /// Captures the current global style for a new battle. Supplied Essences must contain only occupied
    /// slots in ascending visible SlotIndex order; Conduit channels the first entry. When omitted,
    /// the activity's Essence loadout is resolved, using the normal fallback for unassigned activities.
    /// Existing committed snapshots retain their captured style and must not be resolved again.
    /// </summary>
    Task<CombatStyleSnapshot?> ResolveAsync(Guid characterId, EssenceCombatActivity activity, CancellationToken ct,
        IReadOnlyList<PlayerEssence>? equippedEssences = null);
    Task<CombatStyleOperationResult> SelectAsync(Guid characterId, CombatStyleSelectionRequest selection, CancellationToken ct);
    /// <summary>The caller owns the durable reward identity, recipient locking and transaction; attribution is captured at combat time.</summary>
    Task<CombatStyleXpGrantResult> GrantCapturedCombatXpAsync(Guid characterId, string? capturedCombatStyleId, long eligibleBaseXp, CancellationToken ct);
}

public interface ICombatStyleCatalogProvider
{
    CombatStyleCatalog Catalog { get; }
}

public interface ICombatStyleMutationBoundary
{
    /// <summary>Reject committed activities and settle pending idle rewards with the previous build before any selection changes.</summary>
    Task<string?> PrepareMutationAsync(Guid characterId, CancellationToken ct);
}
