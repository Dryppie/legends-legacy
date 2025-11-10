using Application.Common.Interfaces;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Professions.Craftings;
public class CraftingRepository : ICraftingRepository
{
    private readonly IDbContext _dbContext;
    public CraftingRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EquipmentInstance?> RemoveCraftingQueueItemAndReturnItemAsync(Guid characterId, Guid queueItemId, CancellationToken cancellationToken)
    {
        var characterAction = await _dbContext.CharacterActions
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
                    .ThenInclude(cq => cq.EquipmentInstance)
            .FirstOrDefaultAsync(ca => ca.CharacterId == characterId && ca.ActionDetails is CraftingActionDetails, cancellationToken);
        if (characterAction == null || characterAction.ActionDetails == null) return null;

        var queueItem = (characterAction.ActionDetails as CraftingActionDetails).CraftingQueueItems
            .FirstOrDefault(cq => cq.Id == queueItemId);
        if (queueItem == null) return null;

        (characterAction.ActionDetails as CraftingActionDetails).CraftingQueueItems.Remove(queueItem);
        if ((characterAction.ActionDetails as CraftingActionDetails).CraftingQueueItems.Count == 0)
        {
            characterAction.IsDeleted = true;
            characterAction.ActionDetails = null;
        }
        return queueItem?.EquipmentInstance;
    }
}