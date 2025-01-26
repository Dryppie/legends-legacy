using Domain.Interfaces;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Actions;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Damages;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities;
public class EffectActionConverter : JsonConverter<IEffectAction>
{
    public override IEffectAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var jsonDoc = JsonDocument.ParseValue(ref reader))
        {
            var root = jsonDoc.RootElement;
            var actionType = root.GetProperty("Type").GetString();

            switch (actionType)
            {
                case "ApplyStatusEffect":
                    var status = root.GetProperty("Status").GetString()!;
                    return new ApplyStatusEffectAction(status);
                case "Damage":
                    var damageAmount = root.GetProperty("Amount").GetInt32();

                    AttributeType? damageScalingAttribute = null;
                    float damageScalingMultiplier = 0;

                    if (root.TryGetProperty("ScalingAttribute", out var damageScalingAttrElement))
                    {
                        var damageScalingAttrStr = damageScalingAttrElement.GetString()!;
                        damageScalingAttribute = Enum.Parse<AttributeType>(damageScalingAttrStr, ignoreCase: true);
                    }

                    if (root.TryGetProperty("ScalingMultiplier", out var damageScalingMultElement))
                    {
                        damageScalingMultiplier = damageScalingMultElement.GetSingle();
                    }

                    return new DamageAction(damageAmount, damageScalingAttribute, damageScalingMultiplier);

                case "Healing":
                    var healAmount = root.GetProperty("Amount").GetInt32();

                    AttributeType? healScalingAttribute = null;
                    float healScalingMultiplier = 0;

                    if (root.TryGetProperty("ScalingAttribute", out var healScalingAttrElement))
                    {
                        var healScalingAttrStr = healScalingAttrElement.GetString()!;
                        healScalingAttribute = Enum.Parse<AttributeType>(healScalingAttrStr, ignoreCase: true);
                    }

                    if (root.TryGetProperty("ScalingMultiplier", out var healScalingMultElement))
                    {
                        healScalingMultiplier = healScalingMultElement.GetSingle();
                    }

                    return new HealingAction(healAmount, healScalingAttribute, healScalingMultiplier);

                case "ModifyAttribute":
                    var attribute = Enum.Parse<AttributeType>(root.GetProperty("Attribute").GetString()!);
                    var amount = root.GetProperty("Amount").GetInt32();
                    var modifierType = Enum.Parse<ModifierType>(root.GetProperty("ModifierType").GetString()!);
                    var attributeModifier = new AttributeModifier(attribute, amount, modifierType);
                    return new ModifyAttributeAction(attributeModifier);

                case "NestedEffect":
                    // "Effects" is expected to be a JSON array of `Effect` objects
                    var effectsArray = root.GetProperty("Effects");
                    var nestedEffects = new List<EffectDefinition>();

                    foreach (var effectElement in effectsArray.EnumerateArray())
                    {
                        // Pass each JSON sub-object to `JsonSerializer.Deserialize<Effect>`
                        // which in turn will handle the `IEffectAction` in effect.Action
                        var effect = JsonSerializer.Deserialize<EffectDefinition>(effectElement.GetRawText(), options);
                        if (effect == null)
                            throw new JsonException("Failed to deserialize Effect in NestedEffectAction.");

                        nestedEffects.Add(effect);
                    }

                    return new NestedEffectAction(nestedEffects);

                case "Summon":
                    var summonId = root.GetProperty("SummonId").GetString()!;
                    var summonDuration = root.GetProperty("SummonDuration").GetInt32();
                    return new SummonAction(summonId, summonDuration);

                default:
                    throw new NotSupportedException($"Unsupported action type: {actionType}");
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, IEffectAction value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}