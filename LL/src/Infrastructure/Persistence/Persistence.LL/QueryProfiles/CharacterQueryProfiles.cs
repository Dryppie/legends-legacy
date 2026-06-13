using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.QueryProfiles;

public static class CharacterQueryProfiles
{
    public static IQueryable<Character> EntireCharacter(this IQueryable<Character> q)
        => q
            .Include(c => c.BaseAttributes)
            .Include(c => c.EssenceLoadouts.Where(x => x.IsActive))
                .ThenInclude(x => x.Slots)
                    .ThenInclude(x => x.PlayerEssence)
            .Include(c => c.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.InstanceModifiers)
            .Include(c => c.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.ItemBase)
                        .ThenInclude(ib => (ib as EquipmentBase)!.AttributeModifiers);

    public static IQueryable<Character> SnapshotReady(this IQueryable<Character> q)
        => q
            .Include(c => c.BaseAttributes)
            .Include(c => c.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.InstanceModifiers)
            .Include(c => c.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.ItemBase)
                        .ThenInclude(ib => (ib as EquipmentBase)!.AttributeModifiers);

    public static IQueryable<Character> Basic(this IQueryable<Character> q)
        => q; // no includes
}
