using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.EssenceItems;

namespace Persistence.LL.Seeds.JsonSeeding.JsonConverters;
public sealed class ItemBaseConverter : JsonConverter<ItemBase>
{
    public override ItemBase? Read(ref Utf8JsonReader reader,
                                   Type typeToConvert,
                                   JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("itemType", out var typeProp))
            throw new JsonException("Missing \"itemType\" discriminator.");

        var discriminator = typeProp.GetString();

        Type concreteType = discriminator switch
        {
            "Equipment" => typeof(EquipmentBase),
            "Essence" => typeof(EssenceItemBase),
            "Resource" => typeof(ItemBase),
            "Consumable" => typeof(ConsumableItemBase),
            _ => throw new JsonException($"Unknown itemType \"{discriminator}\".")
        };

        // ---- create a shallow *copy* of the options and remove *this* converter ----
        var innerOptions = new JsonSerializerOptions(options);
        for (int i = innerOptions.Converters.Count - 1; i >= 0; i--)
        {
            if (innerOptions.Converters[i] is ItemBaseConverter)
            {
                innerOptions.Converters.RemoveAt(i);
                break;
            }
        }

        // ---- deserialize with the safe options ----
        return (ItemBase)JsonSerializer.Deserialize(
                    root.GetRawText(), concreteType, innerOptions)!;
    }

    // You only need Write if you ever *serialize* ItemBase.
    // Otherwise just leave it un-implemented.
    public override void Write(Utf8JsonWriter writer,
                               ItemBase value,
                               JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
}
