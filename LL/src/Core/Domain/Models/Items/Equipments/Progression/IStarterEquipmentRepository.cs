namespace Domain.Models.Items.Equipments.Progression;

public interface IStarterEquipmentRepository
{
    Task<StarterEquipmentGrant?> GetGrantAsync(Guid characterId, StarterEquipmentGrantKind kind, CancellationToken cancellationToken);
    Task<bool> HasInventoryAsync(Guid characterId, CancellationToken cancellationToken);
    void AddGrant(StarterEquipmentGrant grant);
}
