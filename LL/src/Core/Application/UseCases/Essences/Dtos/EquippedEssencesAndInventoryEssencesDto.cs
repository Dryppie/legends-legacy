using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Essences;

namespace Application.UseCases.Essences.Dtos;
public class EquippedEssencesAndInventoryEssencesDto : IMapFrom<EquippedEssencesAndInventoryEssences>
{
    public List<EssenceDetailsDto> EquippedEssences { get; set; } = [];
    public List<EssenceDetailsDto> InventoryEssences { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EquippedEssencesAndInventoryEssences, EquippedEssencesAndInventoryEssencesDto>();
    }
}