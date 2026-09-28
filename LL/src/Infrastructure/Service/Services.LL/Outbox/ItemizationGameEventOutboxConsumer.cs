using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;
using Domain.Models.Analytics;
using Domain.Models.Outbox;

namespace Services.LL.Outbox;

public sealed class ItemizationGameEventOutboxConsumer(
    IItemizationTelemetryRepository repository, JsonSerializerOptions jsonOptions) : IGameEventOutboxConsumer
{
    public string Consumer => "itemization";
    public bool CanHandle(string eventType) => eventType == GameEventTypes.ItemizationObserved;
    public Task HandleAsync(GameEventOutboxMessage message, CancellationToken ct) => repository.RecordAsync(
        JsonSerializer.Deserialize<ItemizationObservation>(message.PayloadJson, jsonOptions)
        ?? throw new InvalidOperationException("Invalid itemization event."), ct);
}
