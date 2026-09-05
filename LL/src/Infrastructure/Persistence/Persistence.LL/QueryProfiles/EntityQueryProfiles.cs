using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.QueryProfiles;

public static class EntityQueryProfiles
{
    public static IQueryable<Entity> CombatReady(this IQueryable<Entity> q)
        => q
            .Include(e => e.BaseAttributes)
            .Include(e => e.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.InstanceModifiers)
            .Include(e => e.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.ItemBase)
                        .ThenInclude(ib => (ib as EquipmentBase)!.AttributeModifiers)
            .Include(e => (e as Creature)!.StatOverrides);
}
