using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceCombatLoadoutResolver
{
    Task<EssenceCombatLoadout> ResolveAsync(Guid characterId, CancellationToken cancellationToken);
    EssenceCombatLoadout Resolve(Guid characterId, IEnumerable<PlayerEssence> equippedEssences);
}

public sealed record EssenceCombatLoadout(
    Guid CharacterId,
    IReadOnlyList<PlayerEssence> EquippedEssences,
    IReadOnlyList<ResolvedCombatAbility> ActiveAbilities,
    IReadOnlyList<ResolvedCombatAbility> PassiveAbilities,
    IReadOnlyList<AttributeModifierBase> AttributeModifiers,
    IReadOnlySet<string> Tags)
{
    public IReadOnlyList<ResolvedCombatAbility> Abilities => [.. ActiveAbilities, .. PassiveAbilities];
}

public sealed record ResolvedCombatAbility(
    string AbilityDefinitionId,
    Guid SourcePlayerEssenceId,
    string SourceEssenceDefinitionId,
    string AbilityKind,
    int EssenceLevel,
    IReadOnlySet<string> Tags,
    int Cooldown,
    CombatAbilityInstance Ability);
