using Application.UseCases.Inventories.Dtos;

namespace Application.UseCases.Equipments.Dtos;

public sealed class EquipmentChangeResponseDto
{
    public required List<EquipmentSlotDto> EquipmentSlots { get; init; }
    public required List<InventoryItemDto> InventoryItems { get; init; }
}
