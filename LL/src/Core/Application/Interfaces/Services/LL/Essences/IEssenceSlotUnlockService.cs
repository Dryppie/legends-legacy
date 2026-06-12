namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceSlotUnlockService
{
    int GetUnlockedSlotCount(int characterLevel);
}
