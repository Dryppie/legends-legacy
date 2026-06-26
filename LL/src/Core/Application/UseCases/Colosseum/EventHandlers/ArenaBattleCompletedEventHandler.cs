using Application.Interfaces.Services;
using Application.Interfaces.WebSockets;
using Application.UseCases.Colosseum.Events;
using Application.WebSockets.Contracts;
using MediatR;

namespace Application.UseCases.Colosseum.EventHandlers;
public class ArenaBattleCompletedEventHandler : INotificationHandler<ArenaBattleCompletedEvent>
{
    private readonly IGameEventPublisher _eventPublisher;

    public ArenaBattleCompletedEventHandler(
        IGameEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(ArenaBattleCompletedEvent notification, CancellationToken cancellationToken)
    {
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
