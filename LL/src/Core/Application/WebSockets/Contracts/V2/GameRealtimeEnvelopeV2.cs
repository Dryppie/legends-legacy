namespace Application.WebSockets.Contracts.V2;

public sealed class GameRealtimeEnvelopeV2
{
    public required Guid UpdateId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required string Event { get; init; }
    public required object Payload { get; init; }
}
