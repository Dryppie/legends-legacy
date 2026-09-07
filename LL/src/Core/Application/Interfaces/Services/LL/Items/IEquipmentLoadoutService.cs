using Domain.Models.Essences;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Loadouts;
using Domain.Models.Items.Equipments.Slots;

namespace Application.Interfaces.Services.LL.Items;

public interface IEquipmentLoadoutService
{
    Task<List<EquipmentLoadout>> GetAsync(Guid characterId, CancellationToken ct);
    Task<EquipmentEquipResult> SaveAsync(Guid characterId, Guid? id, string name, CancellationToken ct);
    Task<EquipmentEquipResult> DeleteAsync(Guid characterId, Guid id, CancellationToken ct);
    Task<EquipmentEquipResult> ApplyAsync(Guid characterId, Guid id, CancellationToken ct);
    Task<EquipmentEquipResult> SetActivitiesAsync(Guid characterId, Guid id, IReadOnlyList<EssenceCombatActivity> activities, CancellationToken ct);
    Task<List<EquipmentSlot>?> ResolveAsync(Guid characterId, EssenceCombatActivity activity, CancellationToken ct);
}
