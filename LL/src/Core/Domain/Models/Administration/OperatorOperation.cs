namespace Domain.Models.Administration;

// Transport receipt metadata only: no action payload, notes or preview credentials.
public sealed class OperatorOperation
{
    public Guid OperationId { get; set; }
    public string ActorSubject { get; set; } = "";
    public string Environment { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Source { get; set; } = "Game";
    public Guid TargetId { get; set; }
    public string TargetKind { get; set; } = "";
    public string Outcome { get; set; } = "Unknown";
    public int Attempts { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed record OperatorOperationPage(IReadOnlyList<OperatorOperation> Entries, int Total, int Page, int PageSize);
public sealed record OperationDelivery(Guid Id, string Consumer, string Status, int Attempts, DateTimeOffset CreatedAt, Guid? RecoveryOperationId = null);
public sealed record OperationDeliverySummary(int Pending, int Processing, int Processed, int Failed, IReadOnlyList<OperationDelivery> Entries);

public interface IOperatorOperationRepository
{
    Task<bool> BeginAsync(OperatorOperation operation, CancellationToken ct);
    Task FinishAsync(string actor, string environment, Guid id, string outcome, CancellationToken ct);
    Task<OperatorOperationPage> SearchAsync(string actor, string environment, int page, bool unresolvedOnly, CancellationToken ct);
    Task<OperatorOperation?> GetAsync(string actor, string environment, Guid id, CancellationToken ct);
    Task<OperationDeliverySummary> DeliveriesAsync(Guid id, CancellationToken ct);
}
