using Domain.Interfaces.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects.Actions;
using Domain.Models.Combat.Abilities.Effects.StatusEffects;
using Domain.Models.Combat.Abilities.ResourceCosts;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
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
                case "ApplyStatus":
                    var statusId = root.GetProperty("StatusId").GetString() ?? "";

                    return new ApplyStatusAction(statusId);

                case "ApplyStatusEffect":
                    var status = Enum.Parse<StatusEffectType>(root.GetProperty("Status").GetString()!);
                    int stacks = 0;
                    if (root.TryGetProperty("Stacks", out var stacksElement))
                    {
                        stacks = stacksElement.GetInt32();
                    }

                    return new ApplyStatusEffectAction(status, stacks);

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

                    int lifesteal = 0;
                    if (root.TryGetProperty("Lifesteal", out var lifestealElement))
                    {
                        lifesteal = lifestealElement.GetInt32();
                    }

                    return new DamageAction(damageAmount, damageScalingAttribute, damageScalingMultiplier, lifesteal);

                case "ResourceRestore":
                    var restoreAmount = root.GetProperty("Amount").GetInt32();
                    var resourceType = Enum.Parse<ResourceType>(root.GetProperty("ResourceType").GetString()!);
                    AttributeType? restoreScalingAttribute = null;
                    float restoreScalingMultiplier = 0;

                    if (root.TryGetProperty("ScalingAttribute", out var restoreScalingAttrElement))
                    {
                        var restoreScalingAttrStr = restoreScalingAttrElement.GetString()!;
                        restoreScalingAttribute = Enum.Parse<AttributeType>(restoreScalingAttrStr, ignoreCase: true);
                    }

                    if (root.TryGetProperty("ScalingMultiplier", out var restoreScalingMultElement))
                    {
                        restoreScalingMultiplier = restoreScalingMultElement.GetSingle();
                    }

                    return new ResourceRestoreAction(restoreAmount, resourceType, restoreScalingAttribute, restoreScalingMultiplier);

                case "ModifyAttribute":
                    var attribute = Enum.Parse<AttributeType>(root.GetProperty("Attribute").GetString()!);
                    var amount = root.GetProperty("Amount").GetInt32();
                    var modifierType = Enum.Parse<ModifierType>(root.GetProperty("ModifierType").GetString()!);
                    var attributeModifier = new AbilityAttributeModifier(attribute, amount, modifierType);
                    var stackable = root.TryGetProperty("Stackable", out var stackableElement) && stackableElement.GetBoolean();

                    return new ModifyAttributeAction(attributeModifier, stackable);

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