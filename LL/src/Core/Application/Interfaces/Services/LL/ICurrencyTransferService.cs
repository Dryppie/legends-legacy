using Domain.Models.Entities.Characters;

namespace Application.Interfaces.Services.LL;

public interface ICurrencyTransferService
{
    Task<CinderTransferResult> TransferCindersAsync(
        Guid senderCharacterId,
        string recipientName,
        long amount,
        CancellationToken cancellationToken);
}
