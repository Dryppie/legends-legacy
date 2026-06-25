using Application.Interfaces.WebSockets;
using Application.Interfaces.Services.LL.Prophecies;
using Application.WebSockets.Contracts;
using MediatR;

namespace Application.UseCases.Prophecies.Events;

public sealed record ProphecyProgressNotification(ProphecyProgressEvent ProgressEvent) : INotification;

public sealed class ProphecyProgressNotificationHandler : INotificationHandler<ProphecyProgressNotification>
{
    private readonly IProphecyService _prophecyService;
    private readonly IGameEventPublisher _eventPublisher;

    public ProphecyProgressNotificationHandler(
        IProphecyService prophecyService,
        IGameEventPublisher eventPublisher)
    {
        _prophecyService = prophecyService;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(ProphecyProgressNotification notification, CancellationToken cancellationToken)
    {
        var updates = await _prophecyService.TrackProgressAsync(notification.ProgressEvent, cancellationToken);

        foreach (var update in updates)
        {
            await _eventPublisher.PublishAsync(
                new Audience.Character(update.CharacterId),
                new ProphecyProgressedMsg(
                    update.CharacterId,
                    update.ProphecyId,
                    update.Title,
                    update.Scope,
                    update.SlotType,
                    update.Status,
                    update.CurrentValue,
                    update.TargetValue,
                    update.AmountGained,
                    update.Completed));
        }
    }
}
