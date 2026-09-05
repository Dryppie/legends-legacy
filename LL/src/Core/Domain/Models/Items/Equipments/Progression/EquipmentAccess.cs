namespace Domain.Models.Items.Equipments.Progression;

public sealed record StarterEquipmentAccess(StarterEquipmentGrantKind Kind, bool CanClaim,
    string? UnavailableReason, StarterEquipmentGrant? Grant);

public sealed record EquipmentAccess(bool StarterAcquisitionEnabled,
    bool ProtectedAcquisitionEnabled, bool OrdinaryAcquisitionEnabled,
    IReadOnlyList<StarterEquipmentAccess> Starters)
{
}
