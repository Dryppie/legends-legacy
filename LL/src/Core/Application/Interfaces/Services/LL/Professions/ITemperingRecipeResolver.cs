using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Application.Interfaces.Services.LL.Professions;

public interface ITemperingRecipeResolver
{
    TemperingRecipeDefinition? ResolveFor(EquipmentInstance equipment, string? preferredRecipeId = null);
}
