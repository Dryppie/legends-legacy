using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;
namespace Application.UseCases.Equipments.Dtos;

public sealed class PlainEquipmentRecoveryOptionDto : IMapFrom<PlainEquipmentRecoveryOption>
{
    public string DefinitionId { get; set; } = string.Empty;
    public int Tier { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Entitled { get; set; }
    public int Owned { get; set; }
    public int Missing { get; set; }
    public void Mapping(Profile profile) => profile.CreateMap<PlainEquipmentRecoveryOption, PlainEquipmentRecoveryOptionDto>();
}
