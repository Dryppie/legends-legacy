using Application.Common.Mappings;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Domain.Models.Items.EssenceItems;

namespace Application.UseCases.Essences.Dtos;
public class EssenceItemInstanceDto : ItemInstanceDto, IMapFrom<EssenceItemInstance>
{
    public void Mapping(Profile profile)
    {
        profile.CreateMap<EssenceItemInstance, EssenceItemInstanceDto>();
    }
}