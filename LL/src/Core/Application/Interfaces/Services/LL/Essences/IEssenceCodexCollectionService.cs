using Domain.Models.Essences;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceCodexCollectionService
{
    Task<IReadOnlyList<EssenceCodexEntry>> GetVisibleEntriesAsync(Guid characterId, CancellationToken cancellationToken);
}
