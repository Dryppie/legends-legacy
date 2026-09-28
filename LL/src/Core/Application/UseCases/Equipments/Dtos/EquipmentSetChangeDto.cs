namespace Application.UseCases.Equipments.Dtos;

public sealed record EquipmentSetChangeDto(string SetId, string Name, string BonusId, string Description, bool ActiveBefore, bool ActiveAfter);
