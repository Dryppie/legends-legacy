using Application.Common.Mappings;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Domain.Models.Items.Equipments;

namespace Application.UseCases.Equipments.Dtos;
public class EquipmentInstanceDto : ItemInstanceDto, IMapFrom<EquipmentInstance>
{

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EquipmentInstance, EquipmentInstanceDto>();
    }
}