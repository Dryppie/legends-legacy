namespace Domain.Models.Outbox;

public static class GameEventOutboxDeliveryStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Processed = "Processed";
    public const string Failed = "Failed";
}
