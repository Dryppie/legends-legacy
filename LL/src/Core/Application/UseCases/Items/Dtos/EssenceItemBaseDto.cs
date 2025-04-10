using Application.Common.Mappings;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Domain.Models.Items.EssenceItems;

namespace Application.UseCases.Items.Dtos;
public class EssenceItemBaseDto : ItemBaseDto, IMapFrom<EssenceItemBase>
{
    public EssenceDetailsDto? Essence { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EssenceItemBase, EssenceItemBaseDto>();
    }
}