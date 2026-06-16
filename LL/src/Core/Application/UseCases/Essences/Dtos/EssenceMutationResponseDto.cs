using Application.UseCases.Inventories.Dtos;

namespace Application.UseCases.Essences.Dtos;

public sealed class EssenceMutationResponseDto
{
    public required bool Succeeded { get; init; }
    public required string Message { get; init; }
    public required SoulArchiveDto Archive { get; init; }
    public required List<InventoryItemDto> InventoryItems { get; init; }
    public int? DustGained { get; init; }
    public int? DustSpent { get; init; }
    public int? XpGained { get; init; }
    public int? LevelsGained { get; init; }
    public bool? ReachedTierCap { get; init; }
}
