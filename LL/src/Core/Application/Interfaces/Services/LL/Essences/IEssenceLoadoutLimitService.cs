namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceLoadoutLimitService
{
    int GetLoadoutLimit(Guid characterId);
}
