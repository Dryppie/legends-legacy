using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.UseCases.Inventories.Dtos;

namespace Application.UseCases.Professions.Dtos;

public sealed class RemoveCraftingQueueItemResponseDto
{
    public required IReadOnlyList<InventoryItemDto> InventoryItems { get; init; }
    public CharacterActionDto? CurrentAction { get; init; }
}
