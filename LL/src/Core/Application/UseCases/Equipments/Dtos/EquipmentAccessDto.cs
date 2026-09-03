using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class EquipmentAccessDto : IMapFrom<EquipmentAccess>
{

    public bool StarterAcquisitionEnabled { get; set; }
    public bool ForgeEnabled { get; set; }
    public bool ProtectedAcquisitionEnabled { get; set; }
    public bool BaselineRecoveryEnabled { get; set; }
    public bool OrdinaryAcquisitionEnabled { get; set; }
    public IReadOnlyList<StarterEquipmentAccessDto> Starters { get; set; } = [];
    public void Mapping(Profile profile) => profile.CreateMap<EquipmentAccess, EquipmentAccessDto>();
}
