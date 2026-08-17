using Application.UseCases.Inventories.Dtos;

namespace Application.UseCases.Administration.Dtos;

public sealed record CompensationItemGrantResultDto(
    Guid OperationId,
    Guid AccountId,
    Guid CharacterId,
    string ItemBaseId,
    int Quantity,
    IReadOnlyList<InventoryItemDto> GrantedItems,
    bool WasAlreadyProcessed);
