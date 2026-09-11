using Domain.Models.Essences;

namespace Application.UseCases.Equipments.Dtos;

public sealed record EquipmentLoadoutDto(Guid Id, string Name, IReadOnlyList<EssenceCombatActivity> AutoUseActivities, IReadOnlyList<EquipmentLoadoutSlotDto> Slots, int PresetSlot = 0, bool IsUsable = true);
