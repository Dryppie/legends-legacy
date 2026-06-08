using Application.Common.Mappings;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items;

namespace Application.UseCases.Dungeons.Dtos;

public class RunRewardDto : IMapFrom<RunReward>
{
    public string ItemId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public int Quantity { get; set; }
    public string Source { get; set; } = string.Empty;
}
