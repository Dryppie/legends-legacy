using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class StarterEquipmentAccessDto : IMapFrom<StarterEquipmentAccess>
{
    public StarterEquipmentGrantKind Kind { get; set; }
    public bool CanClaim { get; set; }
    public string? UnavailableReason { get; set; }
    public StarterEquipmentGrantDto? Grant { get; set; }
    public void Mapping(Profile profile) => profile.CreateMap<StarterEquipmentAccess, StarterEquipmentAccessDto>();
}
