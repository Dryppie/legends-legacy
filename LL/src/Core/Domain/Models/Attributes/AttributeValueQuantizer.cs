namespace Domain.Models.Attributes;

/// <summary>
/// Converts calculated equipment values into the canonical precision used by the
/// attribute system. Budget allocation can retain full precision internally, but
/// values that leave that calculation should pass through this boundary before
/// they are persisted or presented as a possible roll.
/// </summary>
public static class AttributeValueQuantizer
{
    public static float Quantize(AttributeType attributeType, float value) =>
        (float)Quantize(attributeType, (double)value);

    public static double Quantize(AttributeType attributeType, double value)
    {
        var precision = AttributeCatalog.Get(attributeType).EquipmentDisplayPrecision;
        return Math.Round(value, precision, MidpointRounding.AwayFromZero);
    }
}
