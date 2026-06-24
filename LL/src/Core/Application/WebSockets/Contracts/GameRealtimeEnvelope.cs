namespace Application.WebSockets.Contracts;

public sealed class GameRealtimeEnvelope
{
    public required Guid UpdateId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required string Event { get; init; }
    public required object Payload { get; init; }
}
