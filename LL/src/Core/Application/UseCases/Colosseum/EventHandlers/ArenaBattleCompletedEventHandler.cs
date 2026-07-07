using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Colosseum.Events;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using MediatR;

namespace Application.UseCases.Colosseum.EventHandlers;
public class ArenaBattleCompletedEventHandler : INotificationHandler<ArenaBattleCompletedEvent>
{
    private readonly IGameEventPublisher _eventPublisher;
    private readonly IGameEventOutbox _outbox;

    public ArenaBattleCompletedEventHandler(
        IGameEventPublisher eventPublisher,
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

        var msg = new ArenaBattleCompletedMsg(
            notification.CharacterId,
            notification.EnemyId,
            notification.Outcome.ToString(),
            notification.CharacterRatingBefore,
            notification.CharacterRatingAfter,
            notification.EnemyRatingBefore,
            notification.EnemyRatingAfter);

        await _eventPublisher.PublishAsync(new Audience.Character(notification.CharacterId), msg);
        await _eventPublisher.PublishAsync(new Audience.Character(notification.EnemyId), msg);
    }
}
