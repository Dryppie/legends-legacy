using Application.Common.Mappings;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class EquipmentMigrationChoiceDto : IMapFrom<EquipmentMigrationChoice>
{
    public Guid MigrationId { get; set; }
    public IReadOnlyList<EquipmentProgressionItemDto> Options { get; set; } = [];
}
