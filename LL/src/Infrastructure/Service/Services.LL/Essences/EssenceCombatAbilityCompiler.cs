using Domain.Models.AbilityDefinitions;
using Domain.Models.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects.Trigger;
using Domain.Models.Combat.Abilities.Effects.Usages;
using Domain.Models.Combat.Abilities.Triggers;
using Domain.Models.Combat.Abilities.Triggers.TriggerFilters;
using Domain.Models.Essences;

namespace Services.LL.Essences;

internal static class EssenceCombatAbilityCompiler
{
    public static CombatAbilityDefinition Compile(AbilityDefinition ability, PlayerEssence essence, CombatAbilityType type)
    {
        var combatAbility = new CombatAbilityDefinition
        {
            Id = ability.Id,
            Name = ability.Name,
            Description = ability.Description,
            Type = type,
            Cooldown = SecondsToCombatTicks(EssenceProgressionConstants.ScaleActiveCooldownSeconds(ability.CooldownSeconds, essence.AscensionTier)),
            Usage = new UnlimitedUsage(),
            Condition = EssenceCombatConditionMapper.Build(ability.Conditions)
        };

        var triggers = type == CombatAbilityType.Active
            ? [new AbilityTriggerDefinition { Type = "OnAbilityUsed" }]
            : ability.Triggers.Count == 0
                ? [new AbilityTriggerDefinition { Type = "OnCombatStart" }]
                : ability.Triggers;

        foreach (var trigger in triggers)
        {
            var combatTrigger = new Trigger
            {
                Event = EssenceCombatTriggerMapper.Map(trigger.Type),
                Actions = [.. ability.Effects.Select(effect => EssenceCombatEffectMapper.Map(ability, effect, essence))]
            };

            if (type == CombatAbilityType.Active && combatTrigger.Event == TriggerEvent.OnAbilityUsed)
                combatTrigger.Filters.Add(new AbilityIdFilter { AllowedIds = [ability.Id] });

            combatAbility.Triggers.Add(combatTrigger);
        }

        return combatAbility;
    }

    private static int SecondsToCombatTicks(double seconds) => Math.Max(0, (int)Math.Round(seconds * 10));
}
