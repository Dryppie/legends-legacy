using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Prophecies;

namespace Application.UseCases.Prophecies.Dtos;

public sealed class OpenProphecyCacheResponseDto : IMapFrom<ProphecyCacheOpenResult>
{
    public string CacheItemId { get; set; } = string.Empty;
    public ProphecyRewardSnapshotDto Reward { get; set; } = new();
    public List<ProphecyCacheInventoryDto> Caches { get; set; } = [];
}
