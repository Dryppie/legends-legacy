using Domain.Interfaces.Abilities;
using Domain.Models.Abilities.Effects.Usages;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities;
public class UsageConverter : JsonConverter<IUsage>
{
    public override IUsage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var jsonDoc = JsonDocument.ParseValue(ref reader))
        {
            var root = jsonDoc.RootElement;
            var usageType = root.GetProperty("Type").GetString();

            switch (usageType)
            {
                case "LimitedUsage":
                    var limitedUses = root.GetProperty("Uses").GetInt32();
                    return new LimitedUsage(limitedUses);
                case "RechargeUsage":
                    var rechargeUses = root.GetProperty("Uses").GetInt32();
                    var rechargeInterval = root.GetProperty("RechargeInterval").GetInt32();
                    return new RechargeUsage(rechargeUses, rechargeInterval);
                case "UnlimitedUsage":
                    return new UnlimitedUsage();
                default:
                    return new UnlimitedUsage();
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, IUsage value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}