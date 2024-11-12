using Domain.Models.Abilities.Effects.Trigger;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities;
public class TriggerEventConverter : JsonConverter<TriggerEvent>
{
    public override TriggerEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return stringValue switch
        {
            "None" => TriggerEvent.None,
            "OnAttack" => TriggerEvent.OnAttack,
            "OnAttacked" => TriggerEvent.OnAttacked,
            "OnHeal" => TriggerEvent.OnHeal,
            "OnHealed" => TriggerEvent.OnHealed,
            "OnOverhealed" => TriggerEvent.OnOverhealed,
            "OnTickInterval" => TriggerEvent.OnTickInterval,
            "OnAbilityUsed" => TriggerEvent.OnAbilityUsed,
            "OnDeath" => TriggerEvent.OnDeath,
            "OnCriticalHit" => TriggerEvent.OnCriticalHit,
            "OnDodge" => TriggerEvent.OnDodge,
            "OnBlock" => TriggerEvent.OnBlock,
            "OnParry" => TriggerEvent.OnParry,
            "OnBuffApplied" => TriggerEvent.OnBuffApplied,
            "OnDebuffApplied" => TriggerEvent.OnBuffApplied,
            "OnRevived" => TriggerEvent.OnRevived,
            "OnHealthChanged" => TriggerEvent.OnHealthChanged,
            _ => throw new JsonException($"Unknown trigger event: {stringValue}")
        };
    }

    public override void Write(Utf8JsonWriter writer, TriggerEvent value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            TriggerEvent.None => "None",
            TriggerEvent.OnAttack => "OnAttack",
            TriggerEvent.OnAttacked => "OnAttacked",
            TriggerEvent.OnHeal => "OnHeal",
            TriggerEvent.OnHealed => "OnHealed",
            TriggerEvent.OnTickInterval => "OnTickInterval",
            TriggerEvent.OnAbilityUsed => "OnAbilityUsed",
            TriggerEvent.OnDeath => "OnDeath",
            _ => throw new JsonException($"Unknown trigger event: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}