using Application.Interfaces.Services.LL.Guilds;
using Domain.Extensions.Guilds;
using Domain.Models.Economy;
using Domain.Models.Guilds;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;

namespace Services.LL.Guilds;

public class GuildVaultService : IGuildVaultService
{
    private readonly IGuildVaultRepository _repository;
    private readonly IEconomyLedgerRepository _economyLedger;

    public GuildVaultService(
        IGuildVaultRepository repository,
        IEconomyLedgerRepository economyLedger)
    {
        _repository = repository;
        _economyLedger = economyLedger;
    }

    public async Task<GuildOperationResult<GuildVaultMutation>> DonateAsync(Guid characterId, Guid equipmentInstanceId, CancellationToken cancellationToken)
    {
        var member = await _repository.GetMemberAsync(characterId, cancellationToken);
        if (member is null) return GuildOperationResult<GuildVaultMutation>.Fail("You are not in a guild.");

        var inventoryItem = await _repository.GetDonationAsync(characterId, equipmentInstanceId, cancellationToken);
        if (inventoryItem?.ItemInstance is not EquipmentInstance equipment)
            return GuildOperationResult<GuildVaultMutation>.Fail("Only unequipped equipment can be donated.");
        if (equipment.ProgressionData is { } progression &&
            (progression.State.Ownership.OwnerId != characterId || !progression.State.Ownership.CanTradeOrDonate))
            return GuildOperationResult<GuildVaultMutation>.Fail("Only your unbound discoveries can be donated.");
        if (equipment.HasEquipmentProgression && (inventoryItem.IsFavorite || equipment.IsFavorite))
            return GuildOperationResult<GuildVaultMutation>.Fail("Unfavorite this equipment before donating it.");
        if (await _repository.IsEquippedAsync(equipmentInstanceId, cancellationToken))
            return GuildOperationResult<GuildVaultMutation>.Fail("Equipped equipment must be unequipped before it can be donated.");
        if (await _repository.IsInVaultAsync(equipmentInstanceId, cancellationToken))
            return GuildOperationResult<GuildVaultMutation>.Fail("That equipment already belongs to a guild vault.");

        equipment.DonateEquipmentProgressionToGuild(characterId, member.GuildId);
        var vaultItem = new GuildVaultItem
        {
            GuildId = member.GuildId,
            EquipmentInstanceId = equipmentInstanceId,
            DonatedByCharacterId = characterId
        };
        _repository.Donate(inventoryItem, vaultItem);
        await _economyLedger.RecordGuildVaultMovementAsync(
            EconomyEventType.GuildVaultDonation,
            vaultItem.Id,
            member.GuildId,
            member.Character,
            equipment,
            participantIsSender: true,
            source: "guild-vault:donation",
            cancellationToken: cancellationToken);

        return GuildOperationResult<GuildVaultMutation>.Success(new(
            member.GuildId,
            characterId,
            member.Character.Name,
            equipment));
    }

    public async Task<GuildOperationResult<bool>> BorrowAsync(Guid characterId, Guid vaultItemId, CancellationToken cancellationToken)
    {
        var member = await _repository.GetMemberAsync(characterId, cancellationToken);
        if (member is null) return GuildOperationResult<bool>.Fail("You are not in a guild.");
        if (!member.Guild.PermissionsFor(member.Role).CanBorrowVault)
            return GuildOperationResult<bool>.Fail("Your guild role cannot borrow vault equipment.");

        var vaultItem = await _repository.GetVaultItemAsync(vaultItemId, member.GuildId, cancellationToken);
        if (vaultItem is null || vaultItem.GuildId != member.GuildId) return GuildOperationResult<bool>.Fail("Vault equipment was not found.");
        if (vaultItem.BorrowedByCharacterId is not null)
            return GuildOperationResult<bool>.Fail("That equipment is already borrowed.");
        if (await _repository.IsInInventoryAsync(vaultItem.EquipmentInstanceId, cancellationToken)
            || await _repository.IsEquippedAsync(vaultItem.EquipmentInstanceId, cancellationToken))
            return GuildOperationResult<bool>.Fail("That equipment is not currently available.");

        if (vaultItem.EquipmentInstance.ProgressionData is { } data &&
            (data.State.Ownership.Kind != Domain.Models.Items.Equipments.Progression.EquipmentOwnershipKind.GuildOwned
             || data.State.Ownership.OwnerId != member.GuildId))
            return GuildOperationResult<bool>.Fail("That equipment does not belong to this guild.");

        vaultItem.BorrowedByCharacterId = characterId;
        vaultItem.BorrowedAt = DateTimeOffset.UtcNow;
        _repository.AddToInventory(characterId, vaultItem.EquipmentInstanceId);
        await _economyLedger.RecordGuildVaultMovementAsync(
            EconomyEventType.GuildVaultBorrow,
            vaultItem.Id,
            member.GuildId,
            member.Character,
            vaultItem.EquipmentInstance,
            participantIsSender: false,
            source: "guild-vault:borrow",
            cancellationToken: cancellationToken);

        return GuildOperationResult<bool>.Success(true);
    }

    public async Task<GuildOperationResult<bool>> ReturnAsync(Guid characterId, Guid vaultItemId, CancellationToken cancellationToken)
    {
        var vaultItem = await _repository.GetVaultItemAsync(vaultItemId, null, cancellationToken);
        if (vaultItem is null || vaultItem.BorrowedByCharacterId != characterId) return GuildOperationResult<bool>.Fail("You are not borrowing that equipment.");

        var character = await _repository.GetCharacterAsync(characterId, cancellationToken);
        if (character is null) return GuildOperationResult<bool>.Fail("Your character could not be found.");

        await _repository.RemoveFromCharacterAsync(characterId, vaultItem.EquipmentInstanceId, cancellationToken);

        vaultItem.BorrowedByCharacterId = null;
        vaultItem.BorrowedAt = null;
        await _economyLedger.RecordGuildVaultMovementAsync(
            EconomyEventType.GuildVaultReturn,
            vaultItem.Id,
            vaultItem.GuildId,
            character,
            vaultItem.EquipmentInstance,
            participantIsSender: true,
            source: "guild-vault:return",
            cancellationToken: cancellationToken);
        return GuildOperationResult<bool>.Success(true);
    }

    public async Task<GuildOperationResult<GuildVaultMutation>> WithdrawAsync(Guid characterId, Guid vaultItemId, CancellationToken cancellationToken)
    {
        var member = await _repository.GetMemberAsync(characterId, cancellationToken);
        if (member is null) return GuildOperationResult<GuildVaultMutation>.Fail("You are not in a guild.");

        var canWithdraw = member.Role == GuildRole.Leader
            || member.Role == GuildRole.Officer && member.Guild.PermissionsFor(member.Role).CanWithdrawVault;
        if (!canWithdraw)
            return GuildOperationResult<GuildVaultMutation>.Fail("Your guild role cannot withdraw vault equipment.");

        var vaultItem = await _repository.GetVaultItemAsync(vaultItemId, member.GuildId, cancellationToken);
        if (vaultItem is null || vaultItem.GuildId != member.GuildId) return GuildOperationResult<GuildVaultMutation>.Fail("Vault equipment was not found.");
        if (vaultItem.BorrowedByCharacterId is not null)
            return GuildOperationResult<GuildVaultMutation>.Fail("Borrowed equipment must be returned before it can be withdrawn.");

        if (vaultItem.EquipmentInstance.HasEquipmentProgression)
            return GuildOperationResult<GuildVaultMutation>.Fail("Donated equipment is permanent guild property and cannot be withdrawn.");
        await _repository.WithdrawAsync(characterId, vaultItem, cancellationToken);
        await _economyLedger.RecordGuildVaultMovementAsync(
            EconomyEventType.GuildVaultWithdrawal,
            vaultItem.Id,
            member.GuildId,
            member.Character,
            vaultItem.EquipmentInstance,
            participantIsSender: false,
            source: "guild-vault:withdrawal",
            cancellationToken: cancellationToken);

        return GuildOperationResult<GuildVaultMutation>.Success(new(
            member.GuildId,
            characterId,
            member.Character.Name,
            vaultItem.EquipmentInstance));
    }

}
