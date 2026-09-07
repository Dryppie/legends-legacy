using Domain.Models.Items.Equipments.Slots;

namespace Application.UseCases.Equipments.Dtos;

public sealed record EquipmentLoadoutSlotDto(EquipmentSlotType SlotType, Guid? EquipmentInstanceId, EquipmentInstanceDto? EquipmentInstance);
