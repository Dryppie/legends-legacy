namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceLoadoutLimitService
{
    Task<int> GetLoadoutLimitAsync(Guid characterId, CancellationToken ct);
}
