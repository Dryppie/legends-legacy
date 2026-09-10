using Domain.Models.CombatStyles;
using Domain.Models.Essences;

namespace Application.Interfaces.Services.LL.CombatStyles;

/// <summary>Resolves Channeled Essence eligibility from the same prepared Essence ability used in combat.</summary>
public interface IChanneledEssenceResolver
{
    ChanneledEssenceOption Resolve(PlayerEssence essence);
}
