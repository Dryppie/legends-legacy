namespace Domain.Models.Entities.Characters;

using Domain.Models.Transfers;

public enum CinderTransferFailure
{
    None,
    InvalidAmount,
    RecipientNotFound,
    SameRecipient,
    SenderNotFound,
    InsufficientCinders,
    RecipientBalanceOverflow,
    AccountRestricted,
    GuestAccount
}

public sealed record CinderTransferResult(
    Character? Sender,
    Character? Recipient,
    PlayerTransferRecord? TransferRecord,
    CinderTransferFailure Failure)
{
    public bool IsSuccess =>
        Failure == CinderTransferFailure.None && Sender is not null && Recipient is not null;

    public static CinderTransferResult Success(
        Character sender,
        Character recipient,
        PlayerTransferRecord transferRecord) =>
        new(sender, recipient, transferRecord, CinderTransferFailure.None);

    public static CinderTransferResult Fail(CinderTransferFailure failure) =>
        new(null, null, null, failure);
}
