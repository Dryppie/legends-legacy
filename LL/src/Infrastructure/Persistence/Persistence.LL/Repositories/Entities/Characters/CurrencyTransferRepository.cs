using Application.Common.Interfaces;
using Domain.Models.Economy;
using Domain.Models.Entities.Characters;
using Domain.Models.Transfers;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Entities.Characters;

public sealed class CurrencyTransferRepository(IDbContext context) : ICurrencyTransferRepository
{
    public async Task<CinderTransferResult> TransferCindersAsync(
        Guid senderCharacterId,
        Guid recipientCharacterId,
        long amount,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
            return CinderTransferResult.Fail(CinderTransferFailure.InvalidAmount);
        if (senderCharacterId == recipientCharacterId)
            return CinderTransferResult.Fail(CinderTransferFailure.SameRecipient);

        var characters = await context.Characters
            .Include(x => x.ArenaProfile)
            .Include(x => x.EquippedTitleDefinition)
            .Where(x => x.Id == senderCharacterId || x.Id == recipientCharacterId)
            .ToListAsync(cancellationToken);

        var sender = characters.FirstOrDefault(x => x.Id == senderCharacterId);
        if (sender is null)
            return CinderTransferResult.Fail(CinderTransferFailure.SenderNotFound);

        var recipient = characters.FirstOrDefault(x => x.Id == recipientCharacterId);
        if (recipient is null)
            return CinderTransferResult.Fail(CinderTransferFailure.RecipientNotFound);
        if (sender.Cinders < amount)
            return CinderTransferResult.Fail(CinderTransferFailure.InsufficientCinders);
        if (recipient.Cinders > long.MaxValue - amount)
            return CinderTransferResult.Fail(CinderTransferFailure.RecipientBalanceOverflow);

        sender.Cinders -= amount;
        recipient.Cinders += amount;

        var transferRecord = new PlayerTransferRecord
        {
            Kind = PlayerTransferKind.Cinders,
            SenderAccountId = sender.UserId,
            SenderCharacterId = sender.Id,
            SenderCharacterName = sender.Name,
            RecipientAccountId = recipient.UserId,
            RecipientCharacterId = recipient.Id,
            RecipientCharacterName = recipient.Name,
            AssetId = "currency:cinders",
            AssetName = "Cinders",
            Quantity = amount
        };
        context.PlayerTransferHistory.Add(transferRecord);

        var accountCreatedUtc = await context.Users
            .AsNoTracking()
            .Where(x => x.Id == sender.UserId || x.Id == recipient.UserId)
            .ToDictionaryAsync(x => x.Id, x => x.CreatedUtc, cancellationToken);
        context.EconomyLedger.Add(new EconomyLedgerEntry
        {
            EventType = EconomyEventType.DirectCurrencyTransfer,
            AssetType = EconomyAssetType.Currency,
            ReferenceId = transferRecord.Id,
            SenderAccountId = sender.UserId,
            SenderCharacterId = sender.Id,
            SenderAccountCreatedUtc = accountCreatedUtc.TryGetValue(sender.UserId, out var senderCreatedUtc)
                ? senderCreatedUtc
                : null,
            SenderCharacterLevel = sender.Level,
            RecipientAccountId = recipient.UserId,
            RecipientCharacterId = recipient.Id,
            RecipientAccountCreatedUtc = accountCreatedUtc.TryGetValue(recipient.UserId, out var recipientCreatedUtc)
                ? recipientCreatedUtc
                : null,
            RecipientCharacterLevel = recipient.Level,
            AssetId = "currency:cinders",
            AssetName = "Cinders",
            Quantity = amount,
            UnitValue = 1,
            TotalValue = amount,
            Source = "player-wire",
            OccurredAt = transferRecord.OccurredAt
        });

        return CinderTransferResult.Success(sender, recipient, transferRecord);
    }
}
