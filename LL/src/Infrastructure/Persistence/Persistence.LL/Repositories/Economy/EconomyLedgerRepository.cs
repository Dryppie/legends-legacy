using Application.Common.Interfaces;
using Domain.Models.Economy;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Economy;

public sealed class EconomyLedgerRepository(IDbContext context) : IEconomyLedgerRepository
{
    public async Task RecordItemAcquisitionsAsync(
        Guid characterId,
        IReadOnlyCollection<InventoryItem> items,
        string source,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var participant = await context.Characters
            .AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => new { x.Id, x.UserId, x.Level })
            .SingleAsync(cancellationToken);
        var accountCreatedUtc = await GetAccountCreatedUtcAsync(
            participant.UserId,
            cancellationToken);

        foreach (var item in items)
        {
            await context.EconomyLedger.AddAsync(new EconomyLedgerEntry
            {
                EventType = EconomyEventType.ItemAcquisition,
                AssetType = EconomyAssetType.Item,
                ReferenceId = item.ItemInstanceId,
                RecipientAccountId = participant.UserId,
                RecipientCharacterId = participant.Id,
                RecipientAccountCreatedUtc = accountCreatedUtc,
                RecipientCharacterLevel = participant.Level,
                AssetId = item.ItemInstance.ItemBaseId,
                AssetName = item.ItemInstance.ItemBase.Name,
                DestinationItemInstanceId = item.ItemInstanceId,
                Quantity = item.Quantity,
                Source = source,
                OccurredAt = occurredAt
            }, cancellationToken);
        }
    }

    public async Task RecordGuildVaultMovementAsync(
        EconomyEventType eventType,
        Guid referenceId,
        Guid guildId,
        Character participant,
        EquipmentInstance equipment,
        bool participantIsSender,
        string source,
        CancellationToken cancellationToken)
    {
        var accountCreatedUtc = await GetAccountCreatedUtcAsync(
            participant.UserId,
            cancellationToken);
        var entry = new EconomyLedgerEntry
        {
            EventType = eventType,
            AssetType = EconomyAssetType.Item,
            ReferenceId = referenceId,
            GuildId = guildId,
            AssetId = equipment.ItemBaseId,
            AssetName = equipment.ItemBase.Name,
            SourceItemInstanceId = equipment.Id,
            DestinationItemInstanceId = equipment.Id,
            Quantity = 1,
            Source = source
        };

        if (participantIsSender)
        {
            entry.SenderAccountId = participant.UserId;
            entry.SenderCharacterId = participant.Id;
            entry.SenderAccountCreatedUtc = accountCreatedUtc;
            entry.SenderCharacterLevel = participant.Level;
        }
        else
        {
            entry.RecipientAccountId = participant.UserId;
            entry.RecipientCharacterId = participant.Id;
            entry.RecipientAccountCreatedUtc = accountCreatedUtc;
            entry.RecipientCharacterLevel = participant.Level;
        }

        await context.EconomyLedger.AddAsync(entry, cancellationToken);
    }

    private Task<DateTime?> GetAccountCreatedUtcAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        context.Users
            .AsNoTracking()
            .Where(x => x.Id == accountId)
            .Select(x => (DateTime?)x.CreatedUtc)
            .SingleOrDefaultAsync(cancellationToken);
}
