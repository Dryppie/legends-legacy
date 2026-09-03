using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class StarterEquipmentOptionDto : IMapFrom<StarterEquipmentOption>
{
    public string DefinitionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EquipmentType EquipmentType { get; set; }
    public IReadOnlyDictionary<Domain.Models.Attributes.AttributeType, float> Stats { get; set; } =
        new Dictionary<Domain.Models.Attributes.AttributeType, float>();
    public void Mapping(Profile profile) => profile.CreateMap<StarterEquipmentOption, StarterEquipmentOptionDto>();
}
