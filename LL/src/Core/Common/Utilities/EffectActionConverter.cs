using Domain.Interfaces;
using Domain.Models.Abilities.Effects.Actions;
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
                case "ApplyStatusEffect":
                    var status = root.GetProperty("Status").GetString()!;
                    return new ApplyStatusEffectAction(status);
                case "Damage":
                    var damageAmount = root.GetProperty("Amount").GetInt32();
                    return new DamageAction(damageAmount);

                case "Healing":
                    var healAmount = root.GetProperty("Amount").GetInt32();
                    return new HealingAction(healAmount);

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