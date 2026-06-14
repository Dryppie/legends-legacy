using Application.Interfaces.Services.LL.Essences;
using Domain.Models.AbilityDefinitions;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;

namespace Services.LL.Essences;

public sealed class EssenceCombatAbilityFactory : IEssenceCombatAbilityFactory
{
    public IReadOnlyList<ResolvedCombatAbility> CreateAbilities(EssenceDefinition definition, PlayerEssence essence)
    {
        var abilities = new List<ResolvedCombatAbility>();

        if (!string.IsNullOrWhiteSpace(definition.ActiveAbility.Id))
        {
            abilities.Add(CreateResolvedCombatAbility(
                definition,
                EssenceEvolutionModifierApplier.Apply(definition.ActiveAbility, definition.Evolution.ActiveAbilityModifiers, essence),
                essence,
                CombatAbilityType.Active));
        }

        if (!string.IsNullOrWhiteSpace(definition.PassiveAbility.Id))
        {
            abilities.Add(CreateResolvedCombatAbility(
                definition,
                EssenceEvolutionModifierApplier.Apply(definition.PassiveAbility, definition.Evolution.PassiveAbilityModifiers, essence),
                essence,
                CombatAbilityType.Passive));
        }

        return abilities;
    }

    private static ResolvedCombatAbility CreateResolvedCombatAbility(
        EssenceDefinition definition,
        AbilityDefinition ability,
        PlayerEssence essence,
        CombatAbilityType type)
    {
        var combatDefinition = EssenceCombatAbilityCompiler.Compile(ability, essence, type);
        var instance = new CombatAbilityInstance(combatDefinition);
        var tags = new HashSet<string>(GetEssenceTags(definition, essence), StringComparer.OrdinalIgnoreCase);

        if (type == CombatAbilityType.Passive) instance.RemainingTimeUntilUse = 0;

        foreach (var tag in ability.Tags)
            tags.Add(tag);

        return new ResolvedCombatAbility(
            ability.Id,
            essence.Id,
            essence.EssenceDefinitionId,
            type.ToString(),
            essence.Level,
            tags,
            combatDefinition.Cooldown,
            instance);
    }

    private static IEnumerable<string> GetEssenceTags(EssenceDefinition definition, PlayerEssence essence) =>
        definition.Tags.Concat(essence.IsEvolved ? definition.Evolution.AddsTags : []);
}
