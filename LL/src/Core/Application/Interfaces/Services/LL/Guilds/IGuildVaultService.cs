using Domain.Models.Items.Equipments;

namespace Application.Interfaces.Services.LL.Guilds;

public sealed record GuildVaultMutation(
    Guid GuildId,
    Guid CharacterId,
    string CharacterName,
    EquipmentInstance Equipment);

public interface IGuildVaultService
{
    Task<GuildOperationResult<GuildVaultMutation>> DonateAsync(Guid characterId, Guid equipmentInstanceId, CancellationToken cancellationToken);
    Task<GuildOperationResult<bool>> BorrowAsync(Guid characterId, Guid vaultItemId, CancellationToken cancellationToken);
    Task<GuildOperationResult<bool>> ReturnAsync(Guid characterId, Guid vaultItemId, CancellationToken cancellationToken);
    Task<GuildOperationResult<GuildVaultMutation>> WithdrawAsync(Guid characterId, Guid vaultItemId, CancellationToken cancellationToken);
}
