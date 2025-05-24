using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.CharacterActions.CharacterActionDetails;

namespace Application.UseCases.CharacterActions.Dtos.Responses.CraftingDtos;
public class CraftingActionDetailsDto : IMapFrom<CraftingActionDetails>
{
    public List<CraftingQueueItemDto> CraftingQueueItems { get; set; } = [];
    public void Mapping(Profile profile)
    {
        profile.CreateMap<CraftingActionDetails, CraftingActionDetailsDto>();
    }
}