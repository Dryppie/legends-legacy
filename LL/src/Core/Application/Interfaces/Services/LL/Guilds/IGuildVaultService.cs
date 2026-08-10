namespace Application.Interfaces.Services.LL.Guilds;

public interface IGuildVaultService
{
    Task<GuildOperationResult<bool>> DonateAsync(Guid characterId, Guid equipmentInstanceId, CancellationToken cancellationToken);
    Task<GuildOperationResult<bool>> BorrowAsync(Guid characterId, Guid vaultItemId, CancellationToken cancellationToken);
    Task<GuildOperationResult<bool>> ReturnAsync(Guid characterId, Guid vaultItemId, CancellationToken cancellationToken);
}
