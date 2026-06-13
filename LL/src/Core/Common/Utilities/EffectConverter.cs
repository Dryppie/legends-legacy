using Domain.Models.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects;
using Domain.Models.Combat.Abilities.Effects.Conditions;
using Domain.Models.Combat.Abilities.Effects.Intervals;
using Domain.Models.Combat.Abilities.Effects.Duration;
using Domain.Models.Combat.Abilities.Effects.Trigger;
using Domain.Models.Combat.Abilities.Effects.Usages;
using Domain.Models.Damages;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Combat.Abilities.Effects.EffectModifications;
using Domain.Models.Attributes.Modifiers;
using Domain.Interfaces.Combat.Abilities;

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
        ICondition condition;
        if (root.TryGetProperty("Condition", out var conditionElement)
            && conditionElement.ValueKind != JsonValueKind.Null
            && conditionElement.ValueKind != JsonValueKind.Undefined)
        {
            condition = JsonSerializer.Deserialize<ICondition>(conditionElement.GetRawText(), options)
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
                       ?? new NoDuration(); // Fallback in case of error
        }
        else
        {
            duration = new NoDuration(); // Fallback in case of error
        }

        IUsage usage;
        if (root.TryGetProperty("Usage", out var usageElement)
            && usageElement.ValueKind != JsonValueKind.Null
            && usageElement.ValueKind != JsonValueKind.Undefined)
        {
            usage = JsonSerializer.Deserialize<IUsage>(usageElement.GetRawText(), options)
                       ?? new UnlimitedUsage(); // Fallback in case of error
        }
        else
        {
            usage = new UnlimitedUsage(); // Fallback in case of error
        }

        var targeting = root.TryGetProperty("Targeting", out var targetingProp)
                            ? Enum.Parse<CombatTargeting>(targetingProp.GetString() ?? "None")
                            : CombatTargeting.None;

        var attackType = root.TryGetProperty("AttackType", out var attackTypeProp)
                            ? Enum.Parse<AttackType>(attackTypeProp.GetString() ?? "None")
                            : AttackType.None;

        var damageType = root.TryGetProperty("DamageType", out var damageTypeProp)
                            ? Enum.Parse<DamageType>(damageTypeProp.GetString() ?? "None")
                            : DamageType.None;

        int chance = 100;
        if (root.TryGetProperty("Chance", out var chanceElement) && chanceElement.ValueKind == JsonValueKind.Number)
        {
            chance = chanceElement.GetInt32();
        }

        // **Effect Modifications Parsing**
        List<EffectModification> effectModifications = new();
        if (root.TryGetProperty("EffectModifiers", out var effectModifiersElement) && effectModifiersElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var modifierElement in effectModifiersElement.EnumerateArray())
            {
                if (!modifierElement.TryGetProperty("Type", out var typeElement) ||
                    !modifierElement.TryGetProperty("Amount", out var amountElement) ||
                    !modifierElement.TryGetProperty("ModifierType", out var modifierTypeElement))
                {
                    continue; // Skip if properties are missing
                }

                if (!Enum.TryParse<EffectModificationType>(typeElement.GetString(), true, out var modificationType))
                {
                    throw new JsonException($"Invalid EffectModificationType: {typeElement.GetString()}");
                }

                int amount = amountElement.GetInt32();
                if (!Enum.TryParse<ModifierType>(modifierTypeElement.GetString(), true, out var modifierType))
                {
                    throw new JsonException($"Invalid ModifierType: {modifierTypeElement.GetString()}");
                }

                effectModifications.Add(new EffectModification
                {
                    Amount = amount,
                    ModifierType = modifierType,
                    EffectModificationType = modificationType
                });
            }
        }

        // 3) Construct the Effect. (Replace placeholders with actual data as needed.)
        var effect = new EffectDefinition(
            action: action,
            duration: duration,
            condition: condition,
            interval: interval,
            usage: usage,
            effectTags: new List<EffectTag>(),
            effectModifications: effectModifications,
            targeting: targeting,
            chance: chance,
            attackType: attackType,
            damageType: damageType
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