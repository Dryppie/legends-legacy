using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Resources;

namespace Application.UseCases.Inventories.Dtos;
public class ResourceInstanceDto : ItemInstanceDto, IMapFrom<ResourceInstance>
{
    public int Quality { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<ResourceInstance, ResourceInstanceDto>();
    }
}