using Application.UseCases.Inventories.Dtos;

namespace Application.UseCases.LootHistory.Dtos;

public sealed record LootHistoryEntryDto(
    Guid Id,
    InventoryItemDto Item,
    string Source,
    DateTimeOffset ReceivedAt);
