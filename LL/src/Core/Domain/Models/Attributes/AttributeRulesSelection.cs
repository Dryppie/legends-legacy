namespace Domain.Models.Attributes;

/// <summary>One live selector shared by awards, preparation, previews and snapshot creation.
/// Keep version 17 until the migration audit and competitive snapshot refresh are complete.</summary>
public sealed class AttributeRulesSelection
{
    public AttributeRulesSelection(int version, int? equipmentBalanceVersion = null)
    {
        AttributeRules.ValidateVersion(version);
        Version = version;
        EquipmentBalanceVersion = equipmentBalanceVersion ?? (version == AttributeRules.LegacyVersion ? 1 : 2);
        if (EquipmentBalanceVersion < 1 || (EquipmentBalanceVersion == 1) != (version == AttributeRules.LegacyVersion))
            throw new ArgumentException("Selected equipment release is incompatible with the live combat rules.");
    }
    public int Version { get; }
    public int EquipmentBalanceVersion { get; }
}
