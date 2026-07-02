using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.WebSockets;
using Application.UseCases.Colosseum.Events;
using Application.WebSockets.Contracts;
using MediatR;

namespace Application.UseCases.Colosseum.EventHandlers;
public class ArenaBattleCompletedEventHandler : INotificationHandler<ArenaBattleCompletedEvent>
{
    private readonly IGameEventPublisher _eventPublisher;
    private readonly IAchievementService _achievementService;

    public ArenaBattleCompletedEventHandler(
        IGameEventPublisher eventPublisher,
        IAchievementService achievementService)
    {
        _eventPublisher = eventPublisher;
        _achievementService = achievementService;
    }

    public async Task Handle(ArenaBattleCompletedEvent notification, CancellationToken cancellationToken)
    {
        await _achievementService.RecordColosseumBattleAsync(
            notification.CharacterId,
            notification.EnemyId,
            notification.Outcome,
            notification.CharacterRatingBefore,
            notification.EnemyRatingBefore,
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
