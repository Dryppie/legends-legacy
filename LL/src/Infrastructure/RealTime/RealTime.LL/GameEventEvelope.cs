namespace RealTime.LL;
public sealed class GameEventEnvelope
{
    public required Guid UpdateId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required string Event { get; init; } = default!;
    public required object Payload { get; init; } = default!;
}
