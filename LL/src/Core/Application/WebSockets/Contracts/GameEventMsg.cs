using Application.UseCases.Inventories.Dtos;

namespace Application.WebSockets.Contracts;
//[MessagePack.Union(0, typeof(SaleCompletedMsg))]
//[MessagePack.Union(1, typeof(GuildApplicationMsg))]
//[MessagePack.Union(2, typeof(GuildBuildingUpgradedMsg))]
//[MessagePack.Union(3, typeof(RiftOpenedMsg))]
public abstract record GameEventMsg;

public record SaleCompletedMsg(Guid ItemId, Guid SellerId, int Price) : GameEventMsg;
public record GuildApplicationMsg(Guid GuildId, Guid PlayerId) : GameEventMsg;
public record GuildBuildingUpgradedMsg(Guid GuildId, string BuildingId) : GameEventMsg;
public record RiftOpenedMsg(Guid ZoneId, DateTimeOffset Time) : GameEventMsg;
//public record LootReceivedMsg(Guid CharacterId, List<InventoryItemDto> Items) : GameEventMsg;
