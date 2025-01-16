using Domain.Interfaces;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Conditions;
using Domain.Models.Abilities.Effects.Intervals;
using Domain.Models.Abilities.Effects.Timed;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Abilities.Effects.Usages;
using Domain.Models.Damages;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities;
public class EffectConverter : JsonConverter<EffectDefinition>
{
    public override EffectDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var action = JsonSerializer.Deserialize<IEffectAction>(root.GetProperty("Action").GetRawText(), options)
                     ?? throw new JsonException("Could not deserialize IEffectAction.");

        IEffectInterval interval;
        if (root.TryGetProperty("Interval", out var intervalElement)
            && intervalElement.ValueKind != JsonValueKind.Null
            && intervalElement.ValueKind != JsonValueKind.Undefined)
        {
            interval = JsonSerializer.Deserialize<IEffectInterval>(intervalElement.GetRawText(), options)
                       ?? new NoInterval(); // Fallback in case of error
        }
        else
        {
            interval = new NoInterval(); // Fallback in case of error
        }

        // Condition
        IEffectCondition condition;
        if (root.TryGetProperty("Condition", out var conditionElement)
            && conditionElement.ValueKind != JsonValueKind.Null
            && conditionElement.ValueKind != JsonValueKind.Undefined)
        {
            condition = JsonSerializer.Deserialize<IEffectCondition>(conditionElement.GetRawText(), options)
                        ?? new NoCondition(); // Fallback in case of error
        }
        else
        {
            condition = new NoCondition(); // Fallback in case of error
        }

        // Duration
        IEffectDuration duration;
        if (root.TryGetProperty("Duration", out var durationElement)
            && durationElement.ValueKind != JsonValueKind.Null
            && durationElement.ValueKind != JsonValueKind.Undefined)
        {
            duration = JsonSerializer.Deserialize<IEffectDuration>(durationElement.GetRawText(), options)
                       ?? new TimedDuration(0); // Fallback in case of error
        }
        else
        {
            duration = new TimedDuration(0); // Fallback in case of error
        }

        IEffectUsage usage;
        if (root.TryGetProperty("Interval", out var usageElement)
            && usageElement.ValueKind != JsonValueKind.Null
            && usageElement.ValueKind != JsonValueKind.Undefined)
        {
            usage = JsonSerializer.Deserialize<IEffectUsage>(usageElement.GetRawText(), options)
                       ?? new UnlimitedUsage(); // Fallback in case of error
        }
        else
        {
            usage = new UnlimitedUsage(); // Fallback in case of error
        }

        var targeting = root.TryGetProperty("Targeting", out var targetingProp)
                            ? Enum.Parse<Targeting>(targetingProp.GetString() ?? "None")
                            : Targeting.None;

        var trigger = root.TryGetProperty("Trigger", out var triggerProp)
                            ? Enum.Parse<TriggerEvent>(triggerProp.GetString() ?? "None")
                            : TriggerEvent.None;

        var triggerTarget = root.TryGetProperty("TriggerTarget", out var triggerTargetProp)
                            ? Enum.Parse<Targeting>(triggerTargetProp.GetString() ?? "None")
                            : Targeting.None;

        bool isFlatAmount = false;
        if (root.TryGetProperty("IsFlatAmount", out var isFlatAmountElement) &&
            (isFlatAmountElement.ValueKind == JsonValueKind.True || isFlatAmountElement.ValueKind == JsonValueKind.False))
        {
            isFlatAmount = isFlatAmountElement.GetBoolean();
        }

        int chance = 100;
        if (root.TryGetProperty("Chance", out var chanceElement) && chanceElement.ValueKind == JsonValueKind.Number)
        {
            chance = chanceElement.GetInt32();
        }

        // 3) Construct the Effect. (Replace placeholders with actual data as needed.)
        var effect = new EffectDefinition(
            action: action,
            duration: duration,
            condition: condition,
            interval: interval,
            usage: usage,
            effectTags: new List<EffectTag>(),
            targeting: targeting,
            trigger: trigger,
            triggerTarget: triggerTarget,
            isFlatAmount: isFlatAmount,
            chance: chance,
            attackType: AttackType.None,
            damageType: DamageType.None
        );

         if (root.TryGetProperty("Log", out var logProp))
            effect.Log = logProp.GetString() ?? string.Empty;

        return effect;
    }

    public override void Write(Utf8JsonWriter writer, EffectDefinition value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}