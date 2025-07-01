namespace RealTime.LL;
public sealed class GameEventEnvelope
{
    public required string Event { get; init; } = default!;
    public required object Payload { get; init; } = default!;
}
