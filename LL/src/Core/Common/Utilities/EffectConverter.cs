using Domain.Interfaces;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Conditions;
using Domain.Models.Abilities.Effects.Interval;
using Domain.Models.Abilities.Effects.Timed;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Damages;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities;
public class EffectConverter : JsonConverter<Effect>
{
    public override Effect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        // 1) The "Action" property will be a complex object with `"Type" : "Damage"`, `"NestedEffect"`, etc.
        //    Let the EffectActionConverter handle that for us:
        var action = JsonSerializer.Deserialize<IEffectAction>(root.GetProperty("Action").GetRawText(), options)
                     ?? throw new JsonException("Could not deserialize IEffectAction.");

        IEffectInterval interval;
        if (root.TryGetProperty("Interval", out var intervalElement)
            && intervalElement.ValueKind != JsonValueKind.Null
            && intervalElement.ValueKind != JsonValueKind.Undefined)
        {
            interval = JsonSerializer.Deserialize<IEffectInterval>(intervalElement.GetRawText(), options)
                       ?? new NoInterval();
        }
        else
        {
            interval = new NoInterval();
        }

        // Condition
        IEffectCondition condition;
        if (root.TryGetProperty("Condition", out var conditionElement)
            && conditionElement.ValueKind != JsonValueKind.Null
            && conditionElement.ValueKind != JsonValueKind.Undefined)
        {
            condition = JsonSerializer.Deserialize<IEffectCondition>(conditionElement.GetRawText(), options)
                        ?? new NoCondition();
        }
        else
        {
            condition = new NoCondition();
        }

        // Duration
        IEffectDuration duration;
        if (root.TryGetProperty("Duration", out var durationElement)
            && durationElement.ValueKind != JsonValueKind.Null
            && durationElement.ValueKind != JsonValueKind.Undefined)
        {
            duration = JsonSerializer.Deserialize<IEffectDuration>(durationElement.GetRawText(), options)
                       ?? new TimedDuration(0);
        }
        else
        {
            duration = new TimedDuration(0);
        }

        // 2) Read other properties
        //    For example, if you store Duration, Condition, Interval, etc., parse them similarly.
        //    If they’re basic enums or simple types, you can parse them inline or with your 
        //    existing converters. For example:
        var targeting = root.TryGetProperty("Targeting", out var targetingProp)
                            ? Enum.Parse<Targeting>(targetingProp.GetString() ?? "None")
                            : Targeting.None;

        var trigger = root.TryGetProperty("Trigger", out var triggerProp)
                            ? Enum.Parse<TriggerEvent>(triggerProp.GetString() ?? "None")
                            : TriggerEvent.None;

        bool applyToSelf = false;
        if (root.TryGetProperty("ApplyToSelf", out var applyToSelfElement) &&
            (applyToSelfElement.ValueKind == JsonValueKind.True || applyToSelfElement.ValueKind == JsonValueKind.False))
        {
            applyToSelf = applyToSelfElement.GetBoolean();
        }

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
        var effect = new Effect(
            action: action,
            duration: duration,
            effectTags: new List<EffectTag>(),
            condition: condition,
            caster: null,
            targeting: targeting,
            trigger: trigger,
            interval: interval,
            applyOnSelf: applyToSelf,
            isFlatAmount: isFlatAmount,
            chance: chance,
            attackType: AttackType.None,
            damageType: DamageType.None
        );

         if (root.TryGetProperty("Log", out var logProp))
            effect.Log = logProp.GetString() ?? string.Empty;

        return effect;
    }

    public override void Write(Utf8JsonWriter writer, Effect value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}