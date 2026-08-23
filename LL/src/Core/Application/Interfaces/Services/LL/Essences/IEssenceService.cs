using Domain.Models.Attributes.Modifiers;
using Domain.Models.Essences;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceService
{
    Task<SoulArchive> GetSoulArchiveAsync(Guid characterId, CancellationToken cancellationToken);
    Task<EssenceLoadouts> GetLoadoutsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<EssenceOperationResult> AbsorbUnboundEssenceAsync(Guid characterId, Guid inventoryItemId, CancellationToken cancellationToken);
    Task<DismantleEssenceResult> DismantleUnboundEssenceAsync(
        Guid characterId,
        Guid inventoryItemId,
        CancellationToken cancellationToken,
        int quantity = 1);
    Task<SpendEssenceDustResult> SpendEssenceDustAsync(Guid characterId, Guid playerEssenceId, int dustAmount, CancellationToken cancellationToken);
    Task<EssenceOperationResult> AscendEssenceAsync(Guid characterId, Guid playerEssenceId, CancellationToken cancellationToken);
    Task<EssenceOperationResult> EvolveEssenceAsync(Guid characterId, Guid playerEssenceId, CancellationToken cancellationToken);
    Task<EssenceLoadout> SaveLoadoutAsync(Guid characterId, SaveEssenceLoadoutRequest request, CancellationToken cancellationToken);
    Task<EssenceOperationResult> SetAutoUseActivitiesAsync(
        Guid characterId,
        Guid loadoutId,
        IReadOnlyCollection<EssenceCombatActivity> activities,
        CancellationToken cancellationToken);
    Task<EssenceOperationResult> DeleteLoadoutAsync(Guid characterId, Guid loadoutId, CancellationToken cancellationToken);
    Task<EssenceOperationResult> SetFavoriteAsync(Guid characterId, Guid playerEssenceId, bool isFavorite, CancellationToken cancellationToken);
    Task GrantCombatXpToAttunedEssencesAsync(Guid characterId, int xp, CancellationToken cancellationToken);
    Task GrantCombatXpToAttunedEssencesAsync(
        Guid characterId,
        int xp,
        EssenceCombatActivity activity,
        CancellationToken cancellationToken) =>
        GrantCombatXpToAttunedEssencesAsync(characterId, xp, cancellationToken);
    Task<IReadOnlyList<AttributeModifierBase>> GetAttunedAttributeModifiersAsync(Guid characterId, CancellationToken cancellationToken);
}
