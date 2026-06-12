using Domain.Interfaces.Combat.Abilities;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat.Abilities.Effects.Actions;
using Domain.Models.Combat.Abilities.Effects.StatusEffects;
using Domain.Models.Combat.Abilities.ResourceCosts;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities;

public class EffectActionConverter : JsonConverter<IEffectAction>
{
    public override IEffectAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;
        var operation = ReadString(root, "Operation")
            ?? ReadString(root, "Type")
            ?? throw new JsonException("Effect action requires an Operation.");

        return NormalizeOperation(operation) switch
        {
            CombatEffectOperation.ApplyStatus => new CombatEffectAction
            {
                Operation = CombatEffectOperation.ApplyStatus,
                StatusId = ReadString(root, "StatusId") ?? ReadString(root, "Status") ?? string.Empty
            },
            CombatEffectOperation.ModifyStatusEffect => new CombatEffectAction
            {
                Operation = CombatEffectOperation.ModifyStatusEffect,
                StatusId = ReadStatusEffect(root),
                Magnitude = ReadInt(root, "Magnitude", ReadInt(root, "Stacks", ReadInt(root, "Amount", 1)))
            },
            CombatEffectOperation.Damage => new CombatEffectAction
            {
                Operation = CombatEffectOperation.Damage,
                Magnitude = ReadInt(root, "Magnitude", ReadInt(root, "Amount", 0)),
                ScalingAttribute = ReadAttribute(root, "ScalingAttribute"),
                ScalingMultiplier = ReadFloat(root, "ScalingMultiplier"),
                LifeStealPercentage = ReadFloat(root, "LifeStealPercentage", ReadFloat(root, "Lifesteal"))
            },
            CombatEffectOperation.RestoreResource => new CombatEffectAction
            {
                Operation = CombatEffectOperation.RestoreResource,
                Magnitude = ReadInt(root, "Magnitude", ReadInt(root, "Amount", 0)),
                Resource = ReadEnum<ResourceType>(root, "Resource") ?? ReadEnum<ResourceType>(root, "ResourceType"),
                ScalingAttribute = ReadAttribute(root, "ScalingAttribute"),
                ScalingMultiplier = ReadFloat(root, "ScalingMultiplier")
            },
            CombatEffectOperation.ModifyAttribute => new CombatEffectAction
            {
                Operation = CombatEffectOperation.ModifyAttribute,
                Attribute = ReadEnum<AttributeType>(root, "Attribute"),
                Magnitude = ReadInt(root, "Magnitude", ReadInt(root, "Amount", 0)),
                ModifierType = ReadEnum<ModifierType>(root, "ModifierType") ?? ModifierType.Flat,
                Stackable = ReadBool(root, "Stackable")
            },
            CombatEffectOperation.RemoveStatus => new CombatEffectAction
            {
                Operation = CombatEffectOperation.RemoveStatus,
                StatusId = ReadString(root, "StatusId") ?? ReadString(root, "Status") ?? string.Empty,
                Magnitude = ReadInt(root, "Magnitude", ReadInt(root, "Stacks", ReadInt(root, "Amount", 1)))
            },
            CombatEffectOperation.Cleanse => new CombatEffectAction { Operation = CombatEffectOperation.Cleanse },
            CombatEffectOperation.Summon => new CombatEffectAction
            {
                Operation = CombatEffectOperation.Summon,
                SummonId = ReadString(root, "SummonId") ?? string.Empty,
                SummonDuration = ReadInt(root, "SummonDuration", 0)
            },
            CombatEffectOperation.SelfDestruct => new CombatEffectAction { Operation = CombatEffectOperation.SelfDestruct },
            CombatEffectOperation.TriggerSecondaryEffect => new CombatEffectAction
            {
                Operation = CombatEffectOperation.TriggerSecondaryEffect,
                SecondaryEffectId = ReadString(root, "SecondaryEffectId") ?? ReadString(root, "StatusId") ?? ReadString(root, "Status"),
                Magnitude = ReadInt(root, "Magnitude", ReadInt(root, "Amount", 0))
            },
            var unsupported => throw new NotSupportedException($"Unsupported effect action operation: {unsupported}")
        };
    }

    public override void Write(Utf8JsonWriter writer, IEffectAction value, JsonSerializerOptions options)
    {
        if (value is not CombatEffectAction action)
            throw new NotSupportedException($"Unsupported effect action type: {value.GetType().Name}");

        JsonSerializer.Serialize(writer, action, options);
    }

    private static AttributeType? ReadAttribute(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element)
            ? Enum.Parse<AttributeType>(element.GetString()!, ignoreCase: true)
            : null;

    private static float ReadFloat(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element) ? element.GetSingle() : 0;

    private static float ReadFloat(JsonElement root, string propertyName, float fallback) =>
        root.TryGetProperty(propertyName, out var element) ? element.GetSingle() : fallback;

    private static int ReadInt(JsonElement root, string propertyName, int fallback) =>
        root.TryGetProperty(propertyName, out var element) ? element.GetInt32() : fallback;

    private static string? ReadString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static bool ReadBool(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.True;

    private static T? ReadEnum<T>(JsonElement root, string propertyName) where T : struct =>
        ReadString(root, propertyName) is { } value
            ? Enum.Parse<T>(value, ignoreCase: true)
            : null;

    private static string ReadStatusEffect(JsonElement root)
    {
        var status = ReadString(root, "StatusId") ?? ReadString(root, "Status") ?? string.Empty;
        return Enum.TryParse<StatusEffectType>(status, ignoreCase: true, out var parsed)
            ? parsed.ToString()
            : status;
    }

    private static string NormalizeOperation(string operation) =>
        operation switch
        {
            "ResourceRestore" => CombatEffectOperation.RestoreResource,
            "ApplyStatusEffect" => CombatEffectOperation.ModifyStatusEffect,
            _ => operation
        };
}
