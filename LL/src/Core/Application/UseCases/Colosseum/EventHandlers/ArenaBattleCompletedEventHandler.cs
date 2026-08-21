using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Colosseum.Events;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using MediatR;

namespace Application.UseCases.Colosseum.EventHandlers;
public class ArenaBattleCompletedEventHandler : INotificationHandler<ArenaBattleCompletedEvent>
{
    private readonly IGameRealtimeBroadcaster _eventPublisher;
    private readonly IGameEventOutbox _outbox;

    public ArenaBattleCompletedEventHandler(
        IGameRealtimeBroadcaster eventPublisher,
        IGameEventOutbox outbox)
    {
        _eventPublisher = eventPublisher;
        _outbox = outbox;
    }

    public async Task Handle(ArenaBattleCompletedEvent notification, CancellationToken cancellationToken)
    {
        await _outbox.EnqueueAsync(
            GameEventTypes.ColosseumBattleCompleted,
            new ColosseumBattleCompletedPayload(
                notification.CharacterId,
                notification.EnemyId,
                notification.Outcome,
                notification.CharacterRatingBefore,
                notification.EnemyRatingBefore),
            notification.CharacterId,
            null,
            cancellationToken);

        var message = new ArenaBattleCompleted(
            notification.CharacterId,
            notification.EnemyId,
            notification.Outcome.ToString(),
            notification.CharacterRatingBefore,
            notification.CharacterRatingAfter,
            notification.EnemyRatingBefore,
            notification.EnemyRatingAfter);

        await _eventPublisher.PublishAsync(
            new Audience.Character(notification.CharacterId),
            message,
            nameof(ArenaBattleCompletedEventHandler),
            cancellationToken);
        await _eventPublisher.PublishAsync(
            new Audience.Character(notification.EnemyId),
            message,
            nameof(ArenaBattleCompletedEventHandler),
            cancellationToken);
    }
}
