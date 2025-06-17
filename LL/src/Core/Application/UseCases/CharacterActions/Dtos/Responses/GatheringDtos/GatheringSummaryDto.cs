using Application.Common.Mappings;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Professions;

namespace Application.UseCases.CharacterActions.Dtos.Responses.GatheringDtos;
public class GatheringSummaryDto : IMapFrom<GatheringSummary>
{
    public ProfessionType ProfessionType { get; set; }
    public List<InventoryItemDto> Loot { get; set; } = [];
    public int TotalActions { get; set; }
    public int TotalExperience { get; set; }
    public int TotalSoulstones { get; set; } = 0;
    public void Mapping(Profile profile)
    {
        profile.CreateMap<GatheringSummary, GatheringSummaryDto>();
    }
}
