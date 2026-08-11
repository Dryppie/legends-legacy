namespace Application.Interfaces.Services.LL.Guilds;

public interface IGuildShopService
{
    Task<GuildShopOverviewDto?> GetOverviewAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<GuildOperationResult<GuildShopPurchaseResult>> PurchaseAsync(Guid characterId, string itemKey, DateTimeOffset now, CancellationToken cancellationToken);
}
