using Common.Utilities.EnumConverters;
using Domain.Models.Essences;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities;
public static class EssenceJsonReader
{
    // Cache and reuse JsonSerializerOptions instance
    public static readonly JsonSerializerOptions Options = new()
    {
        Converters =
        {
            new AbilityTypeConverter(),
            new EffectConverter(),
            new EffectModificationTypeConverter(),
            new InterfaceConverterFactory(),
            new ResourceTypeConverter(),
            new TargetingConverter(),
            new TriggerEventConverter(),
            new JsonStringEnumConverter(),
            new TriggerFilterConverter(),
        },
        PropertyNameCaseInsensitive = true,
    };

    public static Essence ReadFromJson(string json)
    {
        return JsonSerializer.Deserialize<Essence>(json, Options)!;
    }
}