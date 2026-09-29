namespace Application.Interfaces.Outbox;

// Scoped to one accepted HTTP execution or one outbox delivery. Never taken from an arbitrary header.
public sealed class AdministrationOperationContext
{
    public Guid? OperationId { get; set; }
}
