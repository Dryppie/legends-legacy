using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Essences;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Essences;
public class EssenceRepository : IEssenceRepository
{
    private readonly IDbContext _context;

    public EssenceRepository(IDbContext context)
    {
        _context = context;
    }
    public async Task<bool> EquipEssence(Guid characterId, Guid essenceItemId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .Include(c => c.EquippedEssences)
            .Include(c => c.Inventory)
                .ThenInclude(inv => inv.InventoryItems)
                    .ThenInclude(ii => ii.Item)
                        .ThenInclude(i => (i as EssenceItem).Essence)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        // Throw if the character does not exist
        NotFoundException.ThrowIfNull(character, nameof(character), characterId);

        var inventoryItem = character.Inventory.InventoryItems
            .FirstOrDefault(ii => ii.Item is EssenceItem ei && ei.Essence.Id.Equals(essenceItemId));

        // Throw if the item was not found
        NotFoundException.ThrowIfNull(inventoryItem, nameof(inventoryItem), essenceItemId);

        var essenceItem = inventoryItem.Item as EssenceItem;
        NotFoundException.ThrowIfNull(essenceItem, nameof(essenceItem), essenceItemId);

        var essence = essenceItem.Essence;
        NotFoundException.ThrowIfNull(essence, nameof(essence), essenceItemId);

        // Check if the character has already equipped this essence
        if (character.EquippedEssences.Any(e => e.Id == essence.Id))
        {
            // You can throw an exception, or return false, or handle it however you'd like.
            throw new InvalidOperationException($"Essence with ID {essence.Id} is already equipped.");
        }

        // 4) Add the essence to the character's EquippedEssences
        character.EquippedEssences.Add(essence);

        // 5) Remove the inventoryItem so it’s no longer in the user’s inventory
        _context.InventoryItems.Remove(inventoryItem);

        // 6) Save changes and return status
        var changes = await _context.SaveChangesAsync(cancellationToken);
        return changes > 0;
    }

    public async Task<bool> DeleteEquippedEssence(Guid characterId, Guid essenceId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .Include(c => c.EquippedEssences)
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId), cancellationToken);

        NotFoundException.ThrowIfNull(character, nameof(character), characterId);

        var essenceToDelete = character.EquippedEssences.First(essence => essence.Id.Equals(essenceId));

        NotFoundException.ThrowIfNull(essenceToDelete, nameof(essenceToDelete), essenceId);

        var removed = character.EquippedEssences.Remove(essenceToDelete);

        await _context.SaveChangesAsync(cancellationToken);

        return removed; // Return true if it was successfully removed
    }

    public async Task<EquippedEssencesAndInventoryEssences> GetEquippedEssencesAndInventoryEssences(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .Include(c => c.EquippedEssences)
            .Include(c => c.Inventory)
                .ThenInclude(inv => inv.InventoryItems)
                    .ThenInclude(ii => ii.Item)
                        .ThenInclude(i => (i as EssenceItem).Essence)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        NotFoundException.ThrowIfNull(character, nameof(character), characterId);

        var equippedEssencesAndInventoryEssences = new EquippedEssencesAndInventoryEssences
        {
            EquippedEssences = [.. character.EquippedEssences],
            InventoryEssences = [.. character.Inventory.InventoryItems.Where(ii => ii.Item is EssenceItem).Select(ii => (ii.Item as EssenceItem).Essence)]
        };

        return equippedEssencesAndInventoryEssences;
    }
}