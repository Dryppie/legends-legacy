using Domain.Models.Items.Equipments;

namespace Application.Interfaces.Services.LL.Items;
public interface IEquipmentService
{
    Task<Equipment> GetEquipmentByIdAsync(Guid equipmentId, CancellationToken cancellationToken);
}
