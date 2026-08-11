using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.CharacterActions.CharacterActionDetails;

namespace Application.UseCases.CharacterActions.Dtos.Responses.CraftingDtos;
public class CraftingActionDetailsDto : IMapFrom<CraftingActionDetails>
{
    public List<CraftingQueueItemDto> CraftingQueueItems { get; set; } = [];
    public void Mapping(Profile profile)
    {
        profile.CreateMap<CraftingActionDetails, CraftingActionDetailsDto>()
            .ForMember(
                destination => destination.CraftingQueueItems,
                options => options.MapFrom(source => source.CraftingQueueItems
                    .OrderBy(item => item.Position)
                    .ThenBy(item => item.AddedAt)
                    .ThenBy(item => item.Id)));
    }
}
