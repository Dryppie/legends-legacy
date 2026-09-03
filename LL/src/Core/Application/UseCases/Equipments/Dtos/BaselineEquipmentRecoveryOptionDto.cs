using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;
public sealed class BaselineEquipmentRecoveryOptionDto : IMapFrom<BaselineEquipmentRecoveryOption>
{
    public StarterEquipmentGrantKind Kind { get; set; }
    public string DefinitionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Entitled { get; set; }
    public int Owned { get; set; }
    public int Missing { get; set; }
    public void Mapping(Profile profile) => profile.CreateMap<BaselineEquipmentRecoveryOption, BaselineEquipmentRecoveryOptionDto>();
}
