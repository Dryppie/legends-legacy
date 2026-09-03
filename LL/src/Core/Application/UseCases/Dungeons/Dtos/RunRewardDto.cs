using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items;

namespace Application.UseCases.Dungeons.Dtos;

public class RunRewardDto : IMapFrom<RunReward>
{
    // Compatibility field for clients released before the equipment naming cleanup.

    public string ItemId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public int Quantity { get; set; }
    public string Source { get; set; } = string.Empty;
    public Application.UseCases.Equipments.Dtos.EquipmentProgressionItemDto? ProgressionData { get; set; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<RunReward, RunRewardDto>();
}
