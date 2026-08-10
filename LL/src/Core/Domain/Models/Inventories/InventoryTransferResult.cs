using Domain.Models.Transfers;

namespace Domain.Models.Inventories;

public enum InventoryTransferFailure
{
    None,
    SameRecipient,
    InvalidQuantity,
    ItemNotFound,
    ItemIsBound,
    NonStackableQuantity,
    InsufficientQuantity,
    BorrowedGuildItem,
    SenderNotFound,
    RecipientNotFound,
    RecipientInventoryNotFound
}

public sealed record InventoryTransferResult(
    InventoryItem? TransferredItem,
    PlayerTransferRecord? TransferRecord,
    InventoryTransferFailure Failure)
{
    public bool IsSuccess =>
        Failure == InventoryTransferFailure.None &&
        TransferredItem is not null &&
        TransferRecord is not null;

    public static InventoryTransferResult Success(
        InventoryItem transferredItem,
        PlayerTransferRecord transferRecord) =>
        new(transferredItem, transferRecord, InventoryTransferFailure.None);

    public static InventoryTransferResult Fail(InventoryTransferFailure failure) =>
        new(null, null, failure);
}
