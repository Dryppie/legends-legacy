using Domain.Models.Attributes;

namespace Domain.Models.Items.Equipments.Progression;

/// <summary>Release-specific prices and allocation shares; combat caps remain combat rules.</summary>
public sealed record EquipmentBalanceSettings(
    int AttributeVersion,
    double CoreShare = .7d,
    double IdentityShare = .1d,
    IReadOnlyDictionary<AttributeType, double>? AttributeCosts = null);

/// <summary>Resolves the exact content release recorded on an item, never the latest release.</summary>
public interface IEquipmentCatalogProvider
{
    IReadOnlyList<int> Versions { get; }
    StarterEquipmentCatalog Get(int version);
}
