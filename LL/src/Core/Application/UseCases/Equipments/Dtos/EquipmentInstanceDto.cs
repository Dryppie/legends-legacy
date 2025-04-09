using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;

namespace Application.UseCases.Equipments.Dtos;
public class EquipmentInstanceDto : IMapFrom<EquipmentInstance>
{
    public Guid Id { get; set; }
    public ItemBase ItemBase { get; set; } = null!;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EquipmentInstance, EquipmentInstanceDto>();
    }
}