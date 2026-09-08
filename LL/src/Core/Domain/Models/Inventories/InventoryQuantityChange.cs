namespace Domain.Models.Inventories;

public sealed record InventoryQuantityChange(Guid CharacterId, string ItemBaseId, long QuantityDelta);
