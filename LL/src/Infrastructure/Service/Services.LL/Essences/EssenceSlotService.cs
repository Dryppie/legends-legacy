using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;

namespace Services.LL.Essences;
public class EssenceSlotService : IEssenceSlotService
{
    private readonly IEssenceSlotRepository _essenceSlotRepository;

    public EssenceSlotService(IEssenceSlotRepository essenceSlotRepository)
    {
        _essenceSlotRepository = essenceSlotRepository;
    }

    public Task<bool> EquipEssence(Essence essence)
    {
        throw new NotImplementedException();
    }

    public void LockSlot()
    {
        throw new NotImplementedException();
    }

    public Task<bool> ToggleActiveReserved(Guid entityId, Guid essenceSlotId, CancellationToken cancellationToken)
    {
        return _essenceSlotRepository.ToggleActiveReserved(entityId, essenceSlotId, cancellationToken);
    }

    public Task<bool> UnequipEssence()
    {
        throw new NotImplementedException();
    }

    public void UnlockSlot()
    {
        throw new NotImplementedException();
    }
}
