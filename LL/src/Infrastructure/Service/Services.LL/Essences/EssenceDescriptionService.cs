using System.Net;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Actions;
using Domain.Models.Attributes;
using Services.LL.Interfaces;

namespace Services.LL.Essences;
public enum TooltipKind { Damage, Heal, Modify }

public record TooltipValue(
    string DisplayValue,    //  "40‑60"
    string BaseValue,       //  "20"
    string? Attribute,      //  "AttackPower" | null
    string AttributeValue,  //  "37"
    string Scale,           //  "0.15"
    string BonusValue,      //  "15"
    TooltipKind Kind);      //  Damage / Heal …

public class EssenceDescriptionService : IEssenceDescriptionService
{
    private const float MAGNITUDE_RANGE = 0.2f;
    private readonly IStatusDefinitionService _statusService;

    public EssenceDescriptionService(IStatusDefinitionService statusService)
    {
        _statusService = statusService;
    }

    public string BuildAbilityDescription(
        AbilityDefinition ability,
        IReadOnlyDictionary<AttributeType, float> attributes)
        => BuildAbilityDescription(
               ability,
               attributes.Select(kvp => new EntityAttribute
               {
                   AttributeType = kvp.Key,
                   Value = kvp.Value
               }).ToList(),
               new HashSet<string>());

    /// <summary>
    /// Generates a final description string from the ability's description template and effects.
    /// This handles multiple effects of the same type by using indexed placeholders like {damage1}, {damage2}, {heal1}, etc.
    /// </summary>
    /// <param name="ability">The ability definition containing a Description and Effects.</param>
    /// <param name="attributes">A dictionary of relevant stats (e.g., Strength, Dex) for scaling calculations.</param>
    /// <returns>The final string with placeholders replaced by computed values.</returns>
    private string BuildAbilityDescription(
        AbilityDefinition ability,
        List<EntityAttribute> attributes,
        HashSet<string> visitedStatuses)
    {
        // Fallback if there's no description
        string template = ability.Description ?? string.Empty;

        // If no effects, just return the raw description
        if (ability.Triggers == null || ability.Triggers.Count == 0)
            return template;

        // We’ll track counters per effect type. 
        // E.g. first damage effect -> {damage1}, second -> {damage2}, etc.
        int damageIndex = 0;
        int healIndex = 0;
        int modifyIndex = 0;
        // ... add more counters as needed.

        // Dictionary to hold placeholder -> finalValue
        var placeholders = new Dictionary<string, TooltipValue>();

        // Parse each effect in order
        foreach (var effect in ability.Triggers.SelectMany(t => t.Actions))
            HandleEffect(effect, placeholders, attributes,
                         ref damageIndex, ref healIndex, ref modifyIndex,
                         visitedStatuses);

        // Finally, replace all placeholders in the template
        return ReplacePlaceholders(template, placeholders);
    }

    private void HandleEffect(
        EffectDefinition effect,
        Dictionary<string, TooltipValue> placeholders,
        List<EntityAttribute> attributes,
        ref int damageIndex, ref int healIndex, ref int modifyIndex,
        HashSet<string> visitedStatuses)
    {
        switch (effect.Action)
        {
            case DamageAction dmg:
                damageIndex++;
                placeholders[$"{{damage{damageIndex}}}"] =
                    BuildTooltipValue(dmg.Magnitude, dmg.ScalingAttribute,
                                      dmg.ScalingMultiplier, attributes, TooltipKind.Damage);
                break;

            case ResourceRestoreAction heal:
                healIndex++;
                placeholders[$"{{heal{healIndex}}}"] =
                    BuildTooltipValue(heal.Magnitude, heal.ScalingAttribute,
                                      heal.ScalingMultiplier, attributes, TooltipKind.Heal);
                break;

            case ApplyStatusAction apply:
                ExpandStatus(apply.StatusId, placeholders, attributes,
                             ref damageIndex, ref healIndex, ref modifyIndex,
                             visitedStatuses);
                break;
        }
    }

    private void ExpandStatus(
    string statusId,
    Dictionary<string, TooltipValue> placeholders,
    List<EntityAttribute> attributes,
    ref int damageIndex, ref int healIndex, ref int modifyIndex,
    HashSet<string> visitedStatuses)
    {
        // Avoid infinite recursion
        if (!visitedStatuses.Add(statusId))
            return;

        if (!_statusService.TryGetById(statusId, out var status))
            return;                 // Unknown status – silently ignore or log

        foreach (var nestedEffect in status.Triggers.SelectMany(t => t.Actions))
            HandleEffect(nestedEffect, placeholders, attributes,
                         ref damageIndex, ref healIndex, ref modifyIndex,
                         visitedStatuses);
    }

    /// <summary>
    /// Example of handling a nested effect by re-using the same indexing logic.
    /// </summary>
    private static void HandleNestedEffect(
        EffectDefinition nestedEffect,
        Dictionary<string, TooltipValue> placeholders,
        List<EntityAttribute> stats,
        ref int damageIndex,
        ref int healIndex,
        ref int modifyIndex)
    {
        var action = nestedEffect.Action;

        switch (action)
        {
            case DamageAction dmg:
                damageIndex++;
                placeholders[$"{{damage{damageIndex}}}"] =
                    BuildTooltipValue(dmg.Magnitude, dmg.ScalingAttribute, dmg.ScalingMultiplier, stats, TooltipKind.Damage);
                break;

            case ResourceRestoreAction heal:
                healIndex++;
                placeholders[$"{{heal{healIndex}}}"] =
                    BuildTooltipValue(heal.Magnitude, heal.ScalingAttribute, heal.ScalingMultiplier, stats, TooltipKind.Heal);
                break;

            //case ApplyStatusAction deeper when deeper.Effects != null:
            //    foreach (var n in deeper.Effects)
            //        HandleNestedEffect(n, placeholders, stats,
            //                           ref damageIndex, ref healIndex, ref modifyIndex);
            //    break;

            default:
                // If your template doesn't need them, skip.
                break;
        }
    }

    /// <summary>
    /// Builds a string showing a range +/- percentage of the given value.
    /// For example, +/-20% of 50 => "40 - 60".
    /// </summary>
    private static TooltipValue BuildTooltipValue(
        int baseValue,
        AttributeType? scalingAttr,
        float scalingMult,
        List<EntityAttribute> stats,
        TooltipKind kind)
    {
        double bonus = 0;
        string? attrName = null;

        var attrValue = 0;
        if (scalingAttr.HasValue)
        {
            var stat = stats.FirstOrDefault(s => s.AttributeType == scalingAttr);
            if (stat != null)
            {
                attrValue = (int)stat.Value;
                bonus = stat.Value * scalingMult;
                attrName = scalingAttr.Value.ToString();
            }
        }

        double total = baseValue + bonus;
        double min = total * (1 - MAGNITUDE_RANGE);
        double max = total * (1 + MAGNITUDE_RANGE);

        return new TooltipValue(
            DisplayValue    : $"{Math.Floor(min)}-{Math.Ceiling(max)}",
            BaseValue       : baseValue.ToString(),
            Attribute       : attrName,
            AttributeValue  : attrValue.ToString(),
            Scale           : scalingMult.ToString(),
            BonusValue      : Math.Round(bonus, 1).ToString(),
            Kind            : kind);
    }

    /// <summary>
    /// Naive string replacement for placeholders. 
    /// E.g. replaces {damage1} with "12", {heal2} with "4", etc.
    /// </summary>
    private static string ReplacePlaceholders(
    string template,
    Dictionary<string, TooltipValue> map)
    {
        string result = template;

        foreach (var (key, tv) in map)
        {
            string cssClass = tv.Kind switch
            {
                TooltipKind.Damage => "dmg",
                TooltipKind.Heal => "heal",
                TooltipKind.Modify => "mod",
                _ => "dmg"
            };

            string span =
                $"<span class=\"{cssClass}\" " +
                $"data-base=\"{WebUtility.HtmlEncode(tv.BaseValue)}\" " +
                $"data-attr=\"{WebUtility.HtmlEncode(tv.Attribute ?? string.Empty)}\" " +
                $"data-attrvalue=\"{WebUtility.HtmlEncode(tv.AttributeValue)}\" " +
                $"data-scale=\"{WebUtility.HtmlEncode(tv.Scale)}\" " +
                $"data-bonus=\"{WebUtility.HtmlEncode(tv.BonusValue)}\" " +
                $"data-display=\"{WebUtility.HtmlEncode(tv.DisplayValue)}\">" +
                $"{WebUtility.HtmlEncode(tv.DisplayValue)}</span>";

            result = result.Replace(key, span);
        }

        return result;
    }
}