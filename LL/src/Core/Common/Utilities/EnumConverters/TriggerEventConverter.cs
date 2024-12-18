using Domain.Models.Abilities.Effects.Trigger;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities.EnumConverters;
public class TriggerEventConverter : JsonConverter<TriggerEvent>
{
    public override TriggerEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return stringValue switch
        {
            "None" => TriggerEvent.None,
            "OnAttack" => TriggerEvent.OnAttack,
            "OnMeleeAttack" => TriggerEvent.OnMeleeAttack,
            "OnRangedAttack" => TriggerEvent.OnRangedAttack,
            "OnAttacked" => TriggerEvent.OnAttacked,
            "OnDamaged" => TriggerEvent.OnDamaged,
            "OnMeleeAttacked" => TriggerEvent.OnMeleeAttacked,
            "OnRangedAttacked" => TriggerEvent.OnRangedAttacked,
            "OnHeal" => TriggerEvent.OnHeal,
            "OnHealed" => TriggerEvent.OnHealed,
            "OnOverhealed" => TriggerEvent.OnOverhealed,
            "OnTickInterval" => TriggerEvent.OnTickInterval,
            "OnAbilityUsed" => TriggerEvent.OnAbilityUsed,
            "OnDeath" => TriggerEvent.OnDeath,
            "OnCriticalHit" => TriggerEvent.OnCriticalHit,
            "OnCriticalHitTaken" => TriggerEvent.OnCriticalHitTaken,
            "OnDodge" => TriggerEvent.OnDodge,
            "OnBlock" => TriggerEvent.OnBlock,
            "OnParry" => TriggerEvent.OnParry,
            "OnBuffApplied" => TriggerEvent.OnBuffApplied,
            "OnBuffRemoved" => TriggerEvent.OnBuffRemoved,
            "OnRevived" => TriggerEvent.OnRevived,
            "OnHealthChanged" => TriggerEvent.OnHealthChanged,
            "OnEffectExpired" => TriggerEvent.OnEffectExpired,
            _ => throw new JsonException($"Unknown trigger event: {stringValue}")
        };
    }

    public override void Write(Utf8JsonWriter writer, TriggerEvent value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            TriggerEvent.None => "None",
            TriggerEvent.OnAttack => "OnAttack",
            TriggerEvent.OnMeleeAttack => "OnMeleeAttack",
            TriggerEvent.OnRangedAttack => "OnRangedAttack",
            TriggerEvent.OnAttacked => "OnAttacked",
            TriggerEvent.OnDamaged => "OnDamaged",
            TriggerEvent.OnMeleeAttacked => "OnMeleeAttacked",
            TriggerEvent.OnRangedAttacked => "OnRangedAttacked",
            TriggerEvent.OnHeal => "OnHeal",
            TriggerEvent.OnHealed => "OnHealed",
            TriggerEvent.OnOverhealed => "OnOverhealed",
            TriggerEvent.OnTickInterval => "OnTickInterval",
            TriggerEvent.OnAbilityUsed => "OnAbilityUsed",
            TriggerEvent.OnDeath => "OnDeath",
            TriggerEvent.OnCriticalHit => "OnCriticalHit",
            TriggerEvent.OnCriticalHitTaken => "OnCriticalHitTaken",
            TriggerEvent.OnDodge => "OnDodge",
            TriggerEvent.OnBlock => "OnBlock",
            TriggerEvent.OnParry => "OnParry",
            TriggerEvent.OnBuffApplied => "OnBuffApplied",
            TriggerEvent.OnBuffRemoved => "OnBuffRemoved",
            TriggerEvent.OnRevived => "OnRevived",
            TriggerEvent.OnHealthChanged => "OnHealthChanged",
            TriggerEvent.OnEffectExpired => "OnEffectExpired",
            _ => throw new JsonException($"Unknown trigger event: {value}")
        };

        writer.WriteStringValue(stringValue);
    }
}