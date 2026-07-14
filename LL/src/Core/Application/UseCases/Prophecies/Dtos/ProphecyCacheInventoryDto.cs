using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Prophecies;
using AutoMapper;

namespace Application.UseCases.Prophecies.Dtos;

public sealed class ProphecyCacheInventoryDto : IMapFrom<ProphecyCacheInventory>
{
    public string ItemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public List<string> PossibleRewards { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ProphecyCacheInventory, ProphecyCacheInventoryDto>();
    }
}
