using Domain.Models.Abilities;
using Domain.Models.Attributes;

namespace Services.LL.Interfaces;
public interface IEssenceDescriptionService
{
    string BuildAbilityDescription(AbilityDefinition ability, Dictionary<AttributeType, float> attributes);
}