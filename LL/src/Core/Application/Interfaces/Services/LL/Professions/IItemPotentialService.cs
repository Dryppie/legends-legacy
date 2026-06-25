using Domain.Models.Items;
using Domain.Models.Items.Equipments;

namespace Application.Interfaces.Services.LL.Professions;

public interface IItemPotentialService
{
    int CalculateStartingPotential(EquipmentBase equipment, int targetTier, ItemQuality quality, int masteryLevel, int craftingLevel);
}
