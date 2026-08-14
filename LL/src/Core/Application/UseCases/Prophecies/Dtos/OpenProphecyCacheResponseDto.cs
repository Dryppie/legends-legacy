using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Prophecies;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;

namespace Application.UseCases.Prophecies.Dtos;

public sealed class OpenProphecyCacheResponseDto : IMapFrom<ProphecyCacheOpenResult>
{
    public string CacheItemId { get; set; } = string.Empty;
    public string CacheTitle { get; set; } = string.Empty;
    public ProphecyRewardSnapshotDto Reward { get; set; } = new();
    public List<InventoryItemDto> Rewards { get; set; } = [];
    public List<ProphecyCacheInventoryDto> Caches { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ProphecyCacheOpenResult, OpenProphecyCacheResponseDto>();
    }
}
