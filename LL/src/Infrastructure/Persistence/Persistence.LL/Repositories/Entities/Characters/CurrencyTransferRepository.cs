using Application.Common.Interfaces;
using Domain.Models.Economy;
using Domain.Models.Entities.Characters;
using Domain.Models.Transfers;
using Domain.Models.Administration;
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

        await context.AcquireCharacterRowsLockAsync(
            [senderCharacterId, recipientCharacterId],
            cancellationToken);

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

        var now = DateTimeOffset.UtcNow;
        var participantAccounts = await context.Users
            .AsNoTracking()
            .Where(x => x.Id == sender.UserId || x.Id == recipient.UserId)
            .Select(x => new
            {
                x.Id,
                x.IsGuest,
                x.CreatedUtc,
                IsRestricted = context.AccountRestrictions.Any(restriction =>
                    restriction.AccountId == x.Id &&
                    restriction.RevokedAt == null &&
                    (restriction.ExpiresAt == null || restriction.ExpiresAt > now) &&
                    (restriction.RestrictionType == AccountRestrictionType.Ban ||
                     restriction.RestrictionType == AccountRestrictionType.MultiplayerRestriction))
            })
            .ToListAsync(cancellationToken);
        if (participantAccounts.Any(x => x.IsGuest))
            return CinderTransferResult.Fail(CinderTransferFailure.GuestAccount);
        if (participantAccounts.Any(x => x.IsRestricted))
            return CinderTransferResult.Fail(CinderTransferFailure.AccountRestricted);
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

        var accountCreatedUtc = participantAccounts.ToDictionary(x => x.Id, x => x.CreatedUtc);
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
