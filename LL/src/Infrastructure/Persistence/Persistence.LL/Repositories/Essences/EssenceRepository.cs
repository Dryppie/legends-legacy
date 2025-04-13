using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;
using Domain.Models.Items.EssenceItems;
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
            .Include(c => c.EssenceSlots)
                .ThenInclude(es => es.OccupiedEssence)
            .Include(c => c.Inventory)
                .ThenInclude(inv => inv.InventoryItems)
                    .ThenInclude(ii => ii.ItemInstance)
                        .ThenInclude(ii => ii.ItemBase)
                            .ThenInclude(ib => (ib as EssenceItemBase).Essence)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        // Throw if the character does not exist
        NotFoundException.ThrowIfNull(character, nameof(character), characterId);

        var inventoryItem = character.Inventory.InventoryItems
            .FirstOrDefault(ii => ii.ItemInstance is EssenceItemInstance ei && ei.ItemBase is EssenceItemBase eib && eib.Essence.Id.Equals(essenceItemId));

        // Throw if the item was not found
        NotFoundException.ThrowIfNull(inventoryItem, nameof(inventoryItem), essenceItemId);

        var essenceItem = inventoryItem.ItemInstance as EssenceItemInstance;
        NotFoundException.ThrowIfNull(essenceItem, nameof(essenceItem), essenceItemId);

        var essence = (essenceItem.ItemBase as EssenceItemBase)!.Essence;
        NotFoundException.ThrowIfNull(essence, nameof(essence), essenceItemId);

        if (inventoryItem.Quantity < 1)
        {
            throw new InvalidOperationException($"You do not have this item in your inventory. Item with ID {inventoryItem.ItemInstanceId}.");
        }

        // Check if the character has already equipped this essence
        if (character.EssenceSlots.Any(es => es.OccupiedEssence?.Id == essence.Id))
        {
            // You can throw an exception, or return false, or handle it however you'd like.
            throw new InvalidOperationException($"Essence with ID {essence.Id} is already equipped.");
        }

        // Check if all active slots already contain an essence
        if (character.EssenceSlots.Where(es => es.SlotState == SlotState.Active).All(es => es.OccupiedEssence != null))
        {
            throw new InvalidOperationException($"You do not have any available Essence Slot to equip Essence with ID {essence.Id}.");
        }

        // 4) Add the essence to the character's first EssenceSlot that is both Active and has no occupied Essence
        character.EssenceSlots.Where(es => es.SlotState == SlotState.Active && es.OccupiedEssence == null).First().OccupiedEssence = essence;

        // Decrease the quantity of the inventory item
        if (inventoryItem.Quantity > 1)
        {
            inventoryItem.Quantity -= 1; // Reduce quantity by 1
        }
        else
        {
            // Remove the inventory item if quantity reaches 0
            _context.InventoryItems.Remove(inventoryItem);
        }

        // 6) Save changes and return status
        var changes = await _context.SaveChangesAsync(cancellationToken);
        return changes > 0;
    }

    public async Task<bool> DeleteEquippedEssence(Guid characterId, Guid essenceId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .Include(c => c.EssenceSlots)
                .ThenInclude(es => es.OccupiedEssence)
            .FirstOrDefaultAsync(c => c.Id.Equals(characterId), cancellationToken);

        NotFoundException.ThrowIfNull(character, nameof(character), characterId);

        character.EssenceSlots.First(es => es.OccupiedEssence != null && es.OccupiedEssence.Id.Equals(essenceId)).OccupiedEssence = null;

        //NotFoundException.ThrowIfNull(essenceSlotToEmpty, nameof(essenceSlotToEmpty), essenceId);

        //var removed = character.EssenceSlots.Remove(essenceToDelete);

        await _context.SaveChangesAsync(cancellationToken);

        return true; // Return true if it was successfully removed
    }

    public async Task<EquippedEssencesAndInventoryEssences> GetEquippedEssencesAndInventoryEssences(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .Include(c => c.EssenceSlots)
                .ThenInclude(es => es.OccupiedEssence)
            .Include(c => c.Inventory)
                .ThenInclude(inv => inv.InventoryItems)
                    .ThenInclude(ii => ii.ItemInstance)
                        .ThenInclude(ii => ii.ItemBase)
                            .ThenInclude(i => (i as EssenceItemBase).Essence)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        NotFoundException.ThrowIfNull(character, nameof(character), characterId);

        var equippedEssencesAndInventoryEssences = new EquippedEssencesAndInventoryEssences
        {
            EquippedEssences = [.. character.EssenceSlots.Where(es => es.OccupiedEssence != null).Select(es => es.OccupiedEssence)],
            InventoryEssences = [.. character.Inventory.InventoryItems
                .Where(ii => ii.ItemInstance is EssenceItemInstance eii && eii.ItemBase is EssenceItemBase)
                    .Select(ii => ((ii.ItemInstance as EssenceItemInstance)!.ItemBase as EssenceItemBase)!.Essence)]
        };

        return equippedEssencesAndInventoryEssences;
    }
}