using Application.UseCases.CharacterActions.Dtos.Responses;
using Domain.Models.Professions.Crafting;

namespace Application.UseCases.Professions.Dtos;

public sealed class MoveCraftingQueueItemRequestDto
{
    public Guid QueueItemId { get; init; }
    public CraftingQueueMoveDirection Direction { get; init; }
}

public sealed class MoveCraftingQueueItemResponseDto
{
    public required CharacterActionDto CurrentAction { get; init; }
}
