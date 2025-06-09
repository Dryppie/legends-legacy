using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;

namespace Application.UseCases.Equipments.Dtos;
public class EquipmentSlotDto : IMapFrom<EquipmentSlot>
{
    public Guid EntityId { get; set; }
    public Guid? EquipmentInstanceId { get; set; }
    public EquipmentInstanceDto? EquipmentInstance { get; set; }
    public EquipmentType EquipmentType { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EquipmentSlot, EquipmentSlotDto>();
    }
}