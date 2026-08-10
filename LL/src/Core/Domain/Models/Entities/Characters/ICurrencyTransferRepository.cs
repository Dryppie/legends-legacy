namespace Domain.Models.Entities.Characters;

public interface ICurrencyTransferRepository
{
    Task<CinderTransferResult> TransferCindersAsync(
        Guid senderCharacterId,
        Guid recipientCharacterId,
        long amount,
        CancellationToken cancellationToken);
}
