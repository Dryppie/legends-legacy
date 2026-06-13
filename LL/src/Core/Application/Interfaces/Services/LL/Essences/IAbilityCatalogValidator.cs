using Domain.Models.AbilityDefinitions;

namespace Application.Interfaces.Services.LL.Essences;

public interface IAbilityCatalogValidator
{
    IReadOnlyList<string> Validate(IReadOnlyList<AbilityDefinition> abilities);
    void ThrowIfInvalid(IReadOnlyList<AbilityDefinition> abilities);
}
