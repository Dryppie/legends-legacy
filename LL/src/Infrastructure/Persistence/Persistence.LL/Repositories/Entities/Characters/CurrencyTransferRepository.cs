using Application.Common.Interfaces;
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

        return CinderTransferResult.Success(sender, recipient, transferRecord);
    }
}
