using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.QueryProfiles;

public static class CharacterQueryProfiles
{
    public static IQueryable<Character> EntireCharacter(this IQueryable<Character> q)
        => q
            .Include(c => c.BaseAttributes)
            .Include(c => c.EssenceSlots)
                .ThenInclude(es => es.OccupiedEssence)
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
            .Include(c => c.EssenceSlots)
                .ThenInclude(es => es.OccupiedEssence)
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