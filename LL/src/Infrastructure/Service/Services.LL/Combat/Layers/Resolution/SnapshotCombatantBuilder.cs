using Application.Common.Interfaces;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Microsoft.EntityFrameworkCore;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Combat.Layers.Resolution;

public sealed class SnapshotCombatantBuilder(
    IDbContext db,
    ICombatSetupService combatSetup) : ISnapshotCombatantBuilder
{
    public async Task<IReadOnlyList<CombatRuntimeParticipant>> BuildAsync(
        IReadOnlyList<SnapshotCombatantRequest> requests,
        CancellationToken cancellationToken)
    {
        var itemBaseIds = requests.SelectMany(x => x.Snapshot.Equipment)
            .Select(x => x.ItemBaseId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var itemBases = await db.ItemBases.AsNoTracking()
            .Include(itemBase => (itemBase as EquipmentBase)!.AttributeModifiers)
            .Where(x => itemBaseIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var participants = new List<CombatRuntimeParticipant>(requests.Count);
        foreach (var request in requests)
        {
            var source = Rehydrate(request.Snapshot, itemBases);
            var combatant = combatSetup.CreatePlayerCombatEntities([source]).Single();
            combatant.EquippedEssences = request.Snapshot.EquippedEssences
                .OrderBy(x => x.SlotIndex)
                .Select(x => x.ToPlayerEssence(request.Snapshot.CharacterId))
                .ToList();
            combatant.HasEquippedEssenceSnapshot = true;
            combatant.Id = request.Slot.SlotId;
            combatant.OriginalId = request.Slot.SourceEntityId;
            participants.Add(new CombatRuntimeParticipant(request.Slot, source, combatant));
        }
        return participants;
    }

    private static Character Rehydrate(
        Domain.Models.Snapshots.CharacterSnapshot snapshot,
        IReadOnlyDictionary<string, Domain.Models.Items.ItemBase> itemBases)
    {
        var character = new Character
        {
            Id = snapshot.CharacterId,
            Name = snapshot.Name,
            ImagePath = snapshot.ImagePath,
            Level = snapshot.Level,
            BaseAttributes = snapshot.BaseAttributes.Select(x => new EntityAttribute
            {
                AttributeType = x.AttributeType,
                Value = x.Value
            }).ToList()
        };

        character.EquipmentSlots = snapshot.Equipment.Select(equipment =>
        {
            if (!itemBases.TryGetValue(equipment.ItemBaseId, out var itemBase) || itemBase is not EquipmentBase equipmentBase)
                throw new InvalidOperationException($"Snapshot equipment definition '{equipment.ItemBaseId}' was not found.");
            var instance = new EquipmentInstance
            {
                Id = equipment.EquipmentInstanceId,
                ItemBaseId = equipment.ItemBaseId,
                ItemBase = equipmentBase,
                BaseRecipeId = equipment.BaseRecipeId,
                BlueprintId = equipment.BlueprintId,
                EquipmentSetId = equipment.EquipmentSetId,
                Rarity = equipment.Rarity,
                Quality = equipment.Quality,
                Tier = equipment.Tier,
                StatModelVersion = equipment.StatModelVersion,
                Potential = equipment.Potential,
                ItemXp = equipment.ItemXp,
                IsMasterpiece = equipment.IsMasterpiece,
                IsLevelingItem = equipment.IsLevelingItem,
                InstanceModifiers = equipment.InstanceModifiers
                    .Select(x => x.ToInstanceModifier(equipment.EquipmentInstanceId))
                    .ToList()
            };
            if (equipment.ProgressionData is { } progressionData)
                instance.ApplyProgressionData(progressionData);
            return new EquipmentSlot
            {
                EntityId = character.Id,
                Entity = character,
                EquipmentInstanceId = instance.Id,
                EquipmentInstance = instance,
                EquipmentSlotType = equipment.Slot
            };
        }).ToList();
        return character;
    }
}
