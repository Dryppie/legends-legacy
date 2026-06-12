using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.EssenceItems;

namespace Application.UseCases.Items.Dtos;
public class EssenceItemBaseDto : ItemBaseDto, IMapFrom<EssenceItemBase>
{
    public string EssenceDefinitionId { get; set; } = string.Empty;
    public int DismantleDustAmount { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EssenceItemBase, EssenceItemBaseDto>();
    }
}