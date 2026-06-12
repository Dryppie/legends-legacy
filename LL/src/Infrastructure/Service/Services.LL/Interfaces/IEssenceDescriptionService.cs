using Domain.Models.Combat.Abilities;
using Domain.Models.Attributes;

namespace Services.LL.Interfaces;
public interface IEssenceDescriptionService
{
    void BuildAbilityDescription(CombatAbilityDefinition ability, IReadOnlyDictionary<AttributeType, float> attributes);
}