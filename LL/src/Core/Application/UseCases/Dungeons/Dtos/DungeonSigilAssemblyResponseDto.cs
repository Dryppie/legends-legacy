using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Dungeons;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;

namespace Application.UseCases.Dungeons.Dtos;

public sealed class DungeonSigilAssemblyResponseDto : IMapFrom<DungeonSigilAssemblyResult>
{
    public string DungeonId { get; set; } = string.Empty;
    public string SigilItemId { get; set; } = string.Empty;
    public string SigilName { get; set; } = string.Empty;
    public int InventoryQuantity { get; set; }
    public long SigilFragmentsRemaining { get; set; }
    public required DungeonHubDto Hub { get; init; }
    public required List<InventoryItemDto> InventoryItems { get; init; }
    public required CharacterDto Character { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DungeonSigilAssemblyResult, DungeonSigilAssemblyResponseDto>()
            .ForMember(destination => destination.Hub, options => options.Ignore())
            .ForMember(destination => destination.InventoryItems, options => options.Ignore())
            .ForMember(destination => destination.Character, options => options.Ignore());
    }
}
