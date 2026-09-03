using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.QueryProfiles;

public static class CharacterQueryProfiles
{
    public static IQueryable<Character> EntireCharacter(this IQueryable<Character> q)
        => q
            .Include(c => c.BaseAttributes)
            .Include(c => c.CharacterAction)
            .Include(c => c.EssenceLoadouts)
                .ThenInclude(x => x.Slots)
                    .ThenInclude(x => x.PlayerEssence)
            .Include(c => c.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.InstanceModifiers)
            .Include(c => c.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.ItemBase)
                        .ThenInclude(ib => (ib as EquipmentBase)!.AttributeModifiers);

}
