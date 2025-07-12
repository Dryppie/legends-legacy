using Domain.Models.Abilities;
using Domain.Models.Attributes;

namespace Services.LL.Interfaces;
public interface IEssenceDescriptionService
{
    void BuildAbilityDescription(AbilityDefinition ability, IReadOnlyDictionary<AttributeType, float> attributes);
}