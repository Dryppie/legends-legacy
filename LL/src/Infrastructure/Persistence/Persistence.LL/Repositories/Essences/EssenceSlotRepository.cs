using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Essences;
public class EssenceSlotRepository : IEssenceSlotRepository
{
    private readonly IDbContext _context;

    public EssenceSlotRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ToggleActiveReserved(Guid entityId, Guid essenceSlotId, CancellationToken cancellationToken)
    {
        var allSlotsForEntity = await _context.EssenceSlots
            .Include(es => es.OccupiedEssence)
            // Subquery grabs the EntityId of the slot we care about
            .Where(es => es.EntityId.Equals(entityId))
            .ToListAsync(cancellationToken);

        // Locate the specific slot to toggling
        var essenceSlot = allSlotsForEntity
            .FirstOrDefault(es => es.Id.Equals(essenceSlotId));

        // Check if the slot exists
        NotFoundException.ThrowIfNull(essenceSlot, nameof(EssenceSlot), essenceSlotId);

        // Check if it actually has an essence
        NotFoundException.ThrowIfNull(essenceSlot.OccupiedEssence, nameof(Essence), essenceSlotId);

        if (essenceSlot.SlotState == SlotState.Locked)
        {
            // TODO: Throw exception to catch for Response.ErrorMessage
            return false;
        }

        // Toggle from Active → Reserved or from Reserved → Active
        if (essenceSlot.SlotState == SlotState.Active)
        {
            // Find a free Reserved slot among this entity’s slots
            var freeReservedSlot = allSlotsForEntity
                .FirstOrDefault(es => es.SlotState == SlotState.Reserved && es.OccupiedEssence == null);

            if (freeReservedSlot == null)
            {
                // No empty Reserved slot available
                // TODO: Throw exception to catch for Response.ErrorMessage
                return false;
            }

            // Move the essence over
            freeReservedSlot.OccupiedEssence = essenceSlot.OccupiedEssence;
            freeReservedSlot.EssenceId = essenceSlot.EssenceId;

            // Clear the old slot
            essenceSlot.OccupiedEssence = null;
            essenceSlot.EssenceId = Guid.Empty;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        else if (essenceSlot.SlotState == SlotState.Reserved)
        {
            // Find a free Active slot among this character’s slots
            var freeActiveSlot = allSlotsForEntity
                .FirstOrDefault(es => es.SlotState == SlotState.Active && es.OccupiedEssence == null);

            if (freeActiveSlot == null)
            {
                // No empty Active slot available
                // TODO: Throw exception to catch for Response.ErrorMessage
                return false;
            }

            // Move the essence over
            freeActiveSlot.OccupiedEssence = essenceSlot.OccupiedEssence;
            freeActiveSlot.EssenceId = essenceSlot.EssenceId;

            // Clear the old slot
            essenceSlot.OccupiedEssence = null;
            essenceSlot.EssenceId = Guid.Empty;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        // If it’s neither an Active nor Reserved slot, just return false.
        // Should not be possibly by any means though, as enum defaults to first value (0 = SlotState.Active)
        return false;
    }

    public Task<bool> EquipEssence(Essence essence)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UnequipEssence()
    {
        throw new NotImplementedException();
    }

    public void LockSlot()
    {
        throw new NotImplementedException();
    }

    public void UnlockSlot()
    {
        throw new NotImplementedException();
    }

    public async Task CreateEssenceSlotOnLevelUp(Guid characterId, SlotState slotState, CancellationToken cancellationToken)
    {
        var newEssenceSlot = new EssenceSlot()
        {
            EntityId = characterId,
            SlotState = slotState,
            SlotType = SlotType.Standard,
            OccupiedEssence = null
        };

        await _context.EssenceSlots.AddAsync(newEssenceSlot, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
