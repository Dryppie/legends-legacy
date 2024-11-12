using Domain.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities;
public class InterfaceConverterFactory : JsonConverterFactory
{
    private readonly Dictionary<Type, JsonConverter> _converters = new();

    public InterfaceConverterFactory()
    {
        // Initialize your converters
        _converters[typeof(IEffectAction)] = new EffectActionConverter();
        _converters[typeof(IEffectDuration)] = new EffectDurationConverter();
        _converters[typeof(IEffectInterval)] = new EffectIntervalConverter();
        _converters[typeof(IEffectCondition)] = new EffectConditionConverter();
        // Add other interface converters as needed
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsInterface && _converters.ContainsKey(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return _converters[typeToConvert];
    }
}