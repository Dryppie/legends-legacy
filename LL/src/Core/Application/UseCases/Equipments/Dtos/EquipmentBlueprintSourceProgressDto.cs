using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class EquipmentBlueprintSourceProgressDto : IMapFrom<EquipmentBlueprintSourceProgress>
{
    public string Name { get; set; } = string.Empty;
    public int Region { get; set; }
    public int CompletionsUntilGuaranteed { get; set; }
    public void Mapping(Profile profile) => profile.CreateMap<EquipmentBlueprintSourceProgress, EquipmentBlueprintSourceProgressDto>();
}
