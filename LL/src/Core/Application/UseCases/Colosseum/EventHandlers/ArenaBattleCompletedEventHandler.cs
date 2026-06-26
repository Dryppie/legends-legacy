using Application.Interfaces.Services;
using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.Services.LL.Colosseum;
using Application.Interfaces.WebSockets;
using Application.UseCases.Colosseum.Events;
using Application.WebSockets.Contracts;
using MediatR;

namespace Application.UseCases.Colosseum.EventHandlers;
public class ArenaBattleCompletedEventHandler : INotificationHandler<ArenaBattleCompletedEvent>
{
    private readonly IGameEventPublisher _eventPublisher;
    private readonly IAchievementService _achievementService;
    private readonly IRatingService _ratingService;
    private readonly IColosseumService _colosseumService;

    public ArenaBattleCompletedEventHandler(
        IRatingService ratingService,
        IColosseumService colosseumService,
        IGameEventPublisher eventPublisher,
        IAchievementService achievementService)
    {
        _eventPublisher = eventPublisher;
        _achievementService = achievementService;
    }

    public async Task Handle(ArenaBattleCompletedEvent notification, CancellationToken cancellationToken)
    {
        var ratingResult = await _ratingService.CalculateNewColosseumRatingsAsync(
            notification.CharacterId, notification.EnemyId, notification.Outcome, cancellationToken);

        await _colosseumService.SaveArenaMatchResult(notification.CharacterId, notification.EnemyId, notification.Outcome, ratingResult, cancellationToken);
        await _achievementService.RecordColosseumBattleAsync(
            notification.CharacterId,
            notification.EnemyId,
            notification.Outcome,
            ratingResult.CharacterARatingBefore,
            ratingResult.CharacterBRatingBefore,
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
