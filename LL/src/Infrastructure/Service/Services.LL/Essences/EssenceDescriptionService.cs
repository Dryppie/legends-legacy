using Domain.Helpers.Constants;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Actions;
using Domain.Models.Attributes;
using Services.LL.Interfaces;

namespace Services.LL.Essences;
public class EssenceDescriptionService : IEssenceDescriptionService
{
    private const float MAGNITUDE_RANGE = 0.2f;
    /// <summary>
    /// Generates a final description string from the ability's description template and effects.
    /// This handles multiple effects of the same type by using indexed placeholders like {damage1}, {damage2}, {heal1}, etc.
    /// </summary>
    /// <param name="ability">The ability definition containing a Description and Effects.</param>
    /// <param name="stats">A dictionary of relevant stats (e.g., Strength, Dex) for scaling calculations.</param>
    /// <returns>The final string with placeholders replaced by computed values.</returns>
    public string BuildAbilityDescription(AbilityDefinition ability, List<EntityAttribute> stats)
    {
        // Fallback if there's no description
        string template = ability.Description ?? string.Empty;

        // If no effects, just return the raw description
        if (ability.Effects == null || ability.Effects.Count == 0)
            return template;

        // We’ll track counters per effect type. 
        // E.g. first damage effect -> {damage1}, second -> {damage2}, etc.
        int damageIndex = 0;
        int healIndex = 0;
        int modifyIndex = 0;
        // ... add more counters as needed.

        // Dictionary to hold placeholder -> finalValue
        var placeholders = new Dictionary<string, string>();

        // Parse each effect in order
        foreach (var effect in ability.Effects)
        {
            var action = effect.Action;
            switch (action)
            {
                case DamageAction damageAction:
                    damageIndex++;
                    // Calculate final damage (if scaling applies)
                    double finalDamage = damageAction.Magnitude;
                    if (damageAction.ScalingAttribute.HasValue)
                    {
                        var scaleValue = stats.FirstOrDefault(ea => ea.AttributeType.Equals(damageAction.ScalingAttribute))!.Value;
                        finalDamage += damageAction.Magnitude + (scaleValue * damageAction.ScalingMultiplier);
                    }
                    // e.g. {damage1}, {damage2}, etc.
                    string dmgKey = $"{{damage{damageIndex}}}";
                    placeholders[dmgKey] = BuildRange(finalDamage);
                    break;

                case HealingAction healingAction:
                    healIndex++;
                    // Calculate final healing if necessary
                    double finalHealing = healingAction.Magnitude;

                    if (healingAction.ScalingAttribute.HasValue)
                    {
                        var scaleValue = stats.FirstOrDefault(ea => ea.AttributeType.Equals(healingAction.ScalingAttribute))!.Value;
                        finalHealing += healingAction.Magnitude + (scaleValue * healingAction.ScalingMultiplier);
                    }

                    string healKey = $"{{heal{healIndex}}}";
                    placeholders[healKey] = BuildRange(finalHealing);
                    break;

                case NestedEffectAction nestedEffectAction:
                    // If you have nested effects, you can recurse or handle them
                    // in a helper method. For now, let's just do it inline:
                    if (nestedEffectAction.Effects != null)
                    {
                        foreach (var nestedEffect in nestedEffectAction.Effects)
                        {
                            // Optionally call the same logic or a helper:
                            HandleNestedEffect(nestedEffect, placeholders, stats, ref damageIndex, ref healIndex, ref modifyIndex);
                        }
                    }
                    break;

                // Add more cases for Summon, ApplyStatusEffect, etc. if you want 
                // them included in the description placeholders.

                default:
                    // If your template doesn't need them, skip.
                    break;
            }
        }

        // Finally, replace all placeholders in the template
        return ReplacePlaceholders(template, placeholders);
    }

    /// <summary>
    /// Example of handling a nested effect by re-using the same indexing logic.
    /// </summary>
    private void HandleNestedEffect(
        EffectDefinition nestedEffect,
        Dictionary<string, string> placeholders,
        List<EntityAttribute> stats,
        ref int damageIndex,
        ref int healIndex,
        ref int modifyIndex)
    {
        var action = nestedEffect.Action;

        switch (action)
        {
            case DamageAction damageAction:
                damageIndex++;
                double finalDamage = damageAction.Magnitude;
                if (damageAction.ScalingAttribute.HasValue)
                {
                    var scaleValue = stats.FirstOrDefault(ea => ea.AttributeType.Equals(damageAction.ScalingAttribute))!.Value;
                    finalDamage += damageAction.Magnitude + (scaleValue * damageAction.ScalingMultiplier);
                }
                string dmgKey = $"{{damage{damageIndex}}}";
                placeholders[dmgKey] = BuildRange(finalDamage);
                break;

            case HealingAction healingAction:
                healIndex++;
                double finalHealing = healingAction.Magnitude;

                if (healingAction.ScalingAttribute.HasValue)
                {
                    var scaleValue = stats.FirstOrDefault(ea => ea.AttributeType.Equals(healingAction.ScalingAttribute))!.Value;
                    finalHealing += healingAction.Magnitude + (scaleValue * healingAction.ScalingMultiplier);
                }
                string healKey = $"{{heal{healIndex}}}";
                placeholders[healKey] = BuildRange(finalHealing);
                break;

            case NestedEffectAction nestedAction:
                // Recursively handle further nesting
                if (nestedAction.Effects != null)
                {
                    foreach (var deeperNested in nestedAction.Effects)
                    {
                        HandleNestedEffect(deeperNested, placeholders, stats, ref damageIndex, ref healIndex, ref modifyIndex);
                    }
                }
                break;

                // etc. handle other cases if desired
        }
    }

    /// <summary>
    /// Helper to scale the base magnitude if a scaling attribute is specified.
    /// </summary>
    private double CalculateScaledValue(double baseValue, AttributeType? scalingAttribute, double scalingMultiplier, List<EntityAttribute> stats)
    {
        double scaledValue = baseValue;

        if (scalingAttribute.HasValue)
        {
            var scaleStat = stats.FirstOrDefault(ea => ea.AttributeType.Equals(scalingAttribute));
            if (scaleStat != null)
            {
                scaledValue += scaleStat.Value * scalingMultiplier;
            }
        }

        return scaledValue;
    }

    /// <summary>
    /// Builds a string showing a range +/- percentage of the given value.
    /// For example, +/-20% of 50 => "40 - 60".
    /// </summary>
    private string BuildRange(double value)
    {
        double min = value * (1.0 - MAGNITUDE_RANGE);
        double max = value * (1.0 + MAGNITUDE_RANGE);

        // Optionally round/floor/ceil as desired:
        return $"{Math.Floor(min)}-{Math.Ceiling(max)}";
    }

    /// <summary>
    /// Naive string replacement for placeholders. 
    /// E.g. replaces {damage1} with "12", {heal2} with "4", etc.
    /// </summary>
    private string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        string result = template;
        foreach (var kvp in placeholders)
        {
            // For example, if you want to color them:
            // string value = $"<color=red>{kvp.Value}</color>";
            // result = result.Replace(kvp.Key, value);

            result = result.Replace(kvp.Key, kvp.Value);
        }
        return result;
    }
}