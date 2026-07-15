using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Dungeons;
using AutoMapper;

namespace Application.UseCases.Dungeons.Dtos;

public sealed class DungeonSigilAssemblyResponseDto : IMapFrom<DungeonSigilAssemblyResult>
{
    public string DungeonId { get; set; } = string.Empty;
    public string SigilItemId { get; set; } = string.Empty;
    public string SigilName { get; set; } = string.Empty;
    public int InventoryQuantity { get; set; }
    public long SigilFragmentsRemaining { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DungeonSigilAssemblyResult, DungeonSigilAssemblyResponseDto>();
    }
}
