using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class EquipmentBlueprintOptionDto : IMapFrom<EquipmentBlueprintOption>
{
    public string StyleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public long Held { get; set; }
    public bool IsCurrent { get; set; }
    public IReadOnlyList<EquipmentBlueprintSourceProgressDto> Sources { get; set; } = [];
    public void Mapping(Profile profile) => profile.CreateMap<EquipmentBlueprintOption, EquipmentBlueprintOptionDto>();
}
