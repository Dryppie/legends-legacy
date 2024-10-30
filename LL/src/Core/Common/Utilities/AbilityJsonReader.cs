using Domain.Models.Abilities;
using System.Text.Json;

namespace Common.Utilities;
public static class AbilityJsonReader
{
    // Cache and reuse JsonSerializerOptions instance
    public static readonly JsonSerializerOptions Options = new()
    {
        Converters =
        {
            new AbilityTypeConverter(),
            new TargetingConverter(),
            new TriggerEventConverter(),
            new InterfaceConverterFactory(),
        },
        PropertyNameCaseInsensitive = true,
    };

    public static Ability ReadFromJson(string json)
    {
        return JsonSerializer.Deserialize<Ability>(json, Options)!;
    }
}