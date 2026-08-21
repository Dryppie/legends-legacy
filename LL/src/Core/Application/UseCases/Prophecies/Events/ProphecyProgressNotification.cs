using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.Interfaces.Services.LL.Prophecies;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using MediatR;

namespace Application.UseCases.Prophecies.Events;

public sealed record ProphecyProgressNotification(ProphecyProgressEvent ProgressEvent) : INotification;

public sealed record ProphecyProgressBatchNotification(IReadOnlyList<ProphecyProgressEvent> ProgressEvents) : INotification;

public sealed class ProphecyProgressNotificationHandler : INotificationHandler<ProphecyProgressNotification>
{
    private readonly IProphecyService _prophecyService;
    private readonly IGameRealtimeBroadcaster _eventPublisher;
    private readonly IGameEventOutbox _outbox;

    public ProphecyProgressNotificationHandler(
        IProphecyService prophecyService,
        IGameRealtimeBroadcaster eventPublisher,
        IGameEventOutbox outbox)
    {
        _prophecyService = prophecyService;
        _eventPublisher = eventPublisher;
        _outbox = outbox;
    }

    public async Task Handle(ProphecyProgressNotification notification, CancellationToken cancellationToken)
    {
        var updates = await _prophecyService.TrackProgressAsync(notification.ProgressEvent, cancellationToken);

        foreach (var update in updates)
        {
            await EnqueueCompletionAsync(update, cancellationToken);
            await _eventPublisher.PublishAsync(
                new Audience.Character(update.CharacterId),
                new ProphecyProgressed(
                    update.CharacterId,
                    update.ProphecyId,
                    update.Title,
                    update.Scope,
                    update.SlotType,
                    update.Status,
                    update.CurrentValue,
                    update.TargetValue,
                    update.AmountGained,
                    update.Completed),
                nameof(ProphecyProgressNotificationHandler),
                cancellationToken);
        }
    }

    private Task EnqueueCompletionAsync(
        ProphecyProgressUpdate update,
        CancellationToken cancellationToken) =>
        !update.Completed || !update.Scope.Equals("Daily", StringComparison.OrdinalIgnoreCase)
            ? Task.CompletedTask
            : _outbox.EnqueueAsync(
                GameEventTypes.ProphecyCompleted,
                new ProphecyCompletedPayload(update.CharacterId, update.ProphecyId, update.Scope),
                update.CharacterId,
                null,
                cancellationToken);
}

public sealed class ProphecyProgressBatchNotificationHandler : INotificationHandler<ProphecyProgressBatchNotification>
{
    private readonly IProphecyService _prophecyService;
    private readonly IGameRealtimeBroadcaster _eventPublisher;
    private readonly IGameEventOutbox _outbox;

    public ProphecyProgressBatchNotificationHandler(
        IProphecyService prophecyService,
        IGameRealtimeBroadcaster eventPublisher,
        IGameEventOutbox outbox)
    {
        _prophecyService = prophecyService;
        _eventPublisher = eventPublisher;
        _outbox = outbox;
    }

    public async Task Handle(ProphecyProgressBatchNotification notification, CancellationToken cancellationToken)
    {
        var updates = await _prophecyService.TrackProgressAsync(notification.ProgressEvents, cancellationToken);

        foreach (var update in updates)
        {
            await EnqueueCompletionAsync(update, cancellationToken);
            await _eventPublisher.PublishAsync(
                new Audience.Character(update.CharacterId),
                new ProphecyProgressed(
                    update.CharacterId,
                    update.ProphecyId,
                    update.Title,
                    update.Scope,
                    update.SlotType,
                    update.Status,
                    update.CurrentValue,
                    update.TargetValue,
                    update.AmountGained,
                    update.Completed),
                nameof(ProphecyProgressBatchNotificationHandler),
                cancellationToken);
        }
    }

    private Task EnqueueCompletionAsync(
        ProphecyProgressUpdate update,
        CancellationToken cancellationToken) =>
        !update.Completed || !update.Scope.Equals("Daily", StringComparison.OrdinalIgnoreCase)
            ? Task.CompletedTask
            : _outbox.EnqueueAsync(
                GameEventTypes.ProphecyCompleted,
                new ProphecyCompletedPayload(update.CharacterId, update.ProphecyId, update.Scope),
                update.CharacterId,
                null,
                cancellationToken);
}
