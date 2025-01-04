using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Essences;

namespace Application.UseCases.Essences.Dtos;
public class EquippedEssencesAndInventoryEssencesDto : IMapFrom<EquippedEssencesAndInventoryEssences>
{
    public List<EssenceDto> EquippedEssences { get; set; } = [];
    public List<EssenceDto> InventoryEssences { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EquippedEssencesAndInventoryEssences, EquippedEssencesAndInventoryEssencesDto>();
    }
}