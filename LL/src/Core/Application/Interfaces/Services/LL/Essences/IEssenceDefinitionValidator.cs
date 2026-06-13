using Domain.Models.Essences.Definitions;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceDefinitionValidator
{
    IReadOnlyList<string> Validate(IReadOnlyList<EssenceDefinition> definitions);
    void ThrowIfInvalid(IReadOnlyList<EssenceDefinition> definitions);
}
