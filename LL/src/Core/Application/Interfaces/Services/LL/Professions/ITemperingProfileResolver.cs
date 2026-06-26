using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Application.Interfaces.Services.LL.Professions;

public interface ITemperingProfileResolver
{
    TemperingProfileDefinition? ResolveFor(EquipmentInstance equipment);
}
