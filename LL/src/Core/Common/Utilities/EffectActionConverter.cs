using Domain.Interfaces;
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

                    if (root.TryGetProperty("DamageScalingAttribute", out var damageScalingAttrElement))
                    {
                        var damageScalingAttrStr = damageScalingAttrElement.GetString()!;
                        damageScalingAttribute = Enum.Parse<AttributeType>(damageScalingAttrStr, ignoreCase: true);
                    }

                    if (root.TryGetProperty("DamageScalingMultiplier", out var damageScalingMultElement))
                    {
                        damageScalingMultiplier = damageScalingMultElement.GetSingle();
                    }

                    // Read AttackType
                    AttackType attackType = AttackType.Melee; // default if not specified
                    if (root.TryGetProperty("AttackType", out var attackTypeElement))
                    {
                        var attackTypeStr = attackTypeElement.GetString()!;
                        attackType = Enum.Parse<AttackType>(attackTypeStr, ignoreCase: true);
                    }

                    // Read DamageType
                    DamageType damageType = DamageType.Physical; // default if not specified
                    if (root.TryGetProperty("DamageType", out var damageTypeElement))
                    {
                        var damageTypeStr = damageTypeElement.GetString()!;
                        damageType = Enum.Parse<DamageType>(damageTypeStr, ignoreCase: true);
                    }

                    // Read DamageTags (if any)
                    var damageTags = new List<DamageTag>();
                    if (root.TryGetProperty("DamageTags", out var damageTagsElement) && damageTagsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var tagElement in damageTagsElement.EnumerateArray())
                        {
                            var tagStr = tagElement.GetString()!;
                            var damageTag = Enum.Parse<DamageTag>(tagStr, ignoreCase: true);
                            damageTags.Add(damageTag);
                        }
                    }

                    return new DamageAction(damageAmount, damageScalingAttribute, damageScalingMultiplier, attackType, damageType, damageTags);



                case "Healing":
                    var healAmount = root.GetProperty("Amount").GetInt32();

                    AttributeType? healScalingAttribute = null;
                    float healScalingMultiplier = 0;

                    if (root.TryGetProperty("DamageScalingAttribute", out var healScalingAttrElement))
                    {
                        var healScalingAttrStr = healScalingAttrElement.GetString()!;
                        damageScalingAttribute = Enum.Parse<AttributeType>(healScalingAttrStr, ignoreCase: true);
                    }

                    if (root.TryGetProperty("DamageScalingMultiplier", out var healScalingMultElement))
                    {
                        damageScalingMultiplier = healScalingMultElement.GetSingle();
                    }

                    return new HealingAction(healAmount, healScalingAttribute, healScalingMultiplier);

                case "ModifyAttribute":
                    var attribute = Enum.Parse<AttributeType>(root.GetProperty("Attribute").GetString()!);
                    var amount = root.GetProperty("Amount").GetInt32();
                    var modifierType = Enum.Parse<ModifierType>(root.GetProperty("ModifierType").GetString()!);
                    var attributeModifier = new AttributeModifier(attribute, amount, modifierType);
                    return new ModifyAttributeAction(attributeModifier);

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