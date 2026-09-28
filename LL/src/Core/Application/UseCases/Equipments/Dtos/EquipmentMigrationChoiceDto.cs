using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class EquipmentMigrationChoiceDto : IMapFrom<EquipmentMigrationChoice>
{
    public Guid MigrationId { get; set; }
    public IReadOnlyList<EquipmentProgressionItemDto> Options { get; set; } = [];
    public void Mapping(Profile profile) => profile.CreateMap<EquipmentMigrationChoice, EquipmentMigrationChoiceDto>();
}
