using Domain.Models.Essences.Definitions;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceCodexCollectionDefinitionProvider
{
    IReadOnlyList<EssenceCodexCollectionDefinition> GetAll();
}
