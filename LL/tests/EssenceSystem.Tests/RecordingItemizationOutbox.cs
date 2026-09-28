using Application.Interfaces.Outbox;
namespace EssenceSystem.Tests;
internal sealed class RecordingItemizationOutbox : IGameEventOutbox
{
    public List<(string Type, object? Payload)> Events { get; } = [];
    public Task EnqueueAsync<TPayload>(string eventType, TPayload payload, Guid? characterId,
        Guid? accountId, CancellationToken ct)
    { Events.Add((eventType, payload)); return Task.CompletedTask; }
}
