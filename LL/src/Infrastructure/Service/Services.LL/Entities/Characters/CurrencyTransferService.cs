using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Entities.Characters;

namespace Services.LL.Entities.Characters;

public sealed class CurrencyTransferService(
    ICharacterRepository characters,
    ICurrencyTransferRepository currencyTransfers,
    ICharacterExperienceProgressionProvider experienceProgression) : ICurrencyTransferService
{
    public async Task<CinderTransferResult> TransferCindersAsync(
        Guid senderCharacterId,
        string recipientName,
        long amount,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
            return CinderTransferResult.Fail(CinderTransferFailure.InvalidAmount);
        if (string.IsNullOrWhiteSpace(recipientName))
            return CinderTransferResult.Fail(CinderTransferFailure.RecipientNotFound);

        var recipientId = await characters.GetCharacterIdByNameAsync(
            recipientName.Trim(),
            cancellationToken);
        if (!recipientId.HasValue)
            return CinderTransferResult.Fail(CinderTransferFailure.RecipientNotFound);

        var result = await currencyTransfers.TransferCindersAsync(
            senderCharacterId,
            recipientId.Value,
            amount,
            cancellationToken);

        if (result.Sender is not null)
        {
            result.Sender.ExperienceUntilNextLevel =
                experienceProgression.GetRequiredExperience(result.Sender.Level);
        }

        if (result.Recipient is not null)
        {
            result.Recipient.ExperienceUntilNextLevel =
                experienceProgression.GetRequiredExperience(result.Recipient.Level);
        }

        return result;
    }
}
