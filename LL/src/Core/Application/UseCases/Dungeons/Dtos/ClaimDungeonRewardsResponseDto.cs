using Application.UseCases.Characters.Dtos;
using Application.UseCases.Inventories.Dtos;

namespace Application.UseCases.Dungeons.Dtos;

public sealed class ClaimDungeonRewardsResponseDto
{
    public DungeonRunDto? ActiveRun { get; init; }
    public required List<InventoryItemDto> InventoryItems { get; init; }
    public required List<InventoryItemDto> ClaimedLoot { get; init; }
    public required CharacterDto Character { get; init; }
    public required DungeonHubDto Hub { get; init; }
}
