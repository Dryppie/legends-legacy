using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items.Equipments;
using Domain.Models.LootTables;
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
                        .ThenInclude(ib => (ib as EquipmentBase)!.AttributeModifiers);

    public static IQueryable<Entity> CombatReadyWithLoot(this IQueryable<Entity> q)
        => q.CombatReady()
            .Include(e => (e as Creature)!.LootTable)
                .ThenInclude(lt => lt.Entries)
                    .ThenInclude(lt => (lt as LootTable)!.Entries)
                        .ThenInclude(lte => (lte as LootTableItem)!.Item);
}
