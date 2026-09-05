using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Inventories;

namespace Application.Interfaces.Services.LL.Items;

public sealed class EquipmentProgressionOptions
{
    public const string SectionName = "EquipmentProgression";
    public bool StarterAcquisitionEnabled { get; set; } = true;
    public bool ProtectedAcquisitionEnabled { get; set; } = true;
    public bool OrdinaryAcquisitionEnabled { get; set; } = true;
}

public sealed record StarterEquipmentClaimResult(StarterEquipmentGrant? Grant, string? Error)
{
    public IReadOnlyList<InventoryItem> Rewards { get; init; } = [];
    public static StarterEquipmentClaimResult Fail(string error) => new(null, error);
}

public interface IStarterEquipmentService
{
    Task<EquipmentAccess> GetAccessAsync(Guid characterId, CancellationToken cancellationToken);
    IReadOnlyList<StarterEquipmentOption> GetOptions();
    Task<StarterEquipmentClaimResult> ClaimAsync(Guid characterId, StarterEquipmentGrantKind kind,
        IReadOnlyList<string> definitionIds, CancellationToken cancellationToken);
}
