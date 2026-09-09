using Domain.Models.CombatStyles;
using Domain.Models.Essences;

namespace Application.Interfaces.Services.LL.CombatStyles;

public interface ICombatStyleService
{
    Task<CombatStyleOverview> GetOverviewAsync(Guid characterId, CancellationToken ct);
    Task<CombatStyleOverview> PreviewAsync(Guid characterId, CombatStyleSelectionRequest selection, CancellationToken ct);
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
