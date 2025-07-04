using Application.Interfaces.Services;
using Application.Interfaces.Services.LL.Colosseum;
using Application.UseCases.Colosseum.Events;
using MediatR;

namespace Application.UseCases.Colosseum.EventHandlers;
public class ArenaBattleCompletedEventHandler : INotificationHandler<ArenaBattleCompletedEvent>
{
    private readonly IRatingService _ratingService;
    private readonly IColosseumService _colosseumService;

    public ArenaBattleCompletedEventHandler(IRatingService ratingService, IColosseumService colosseumService)
    {
        _ratingService = ratingService;
        _colosseumService = colosseumService;
    }

    public async Task Handle(ArenaBattleCompletedEvent notification, CancellationToken cancellationToken)
{
    var ratingResult = await _ratingService.CalculateNewColosseumRatingsAsync(
        notification.CharacterId, notification.EnemyId, notification.Outcome, cancellationToken);

    await _colosseumService.SaveArenaMatchResult(notification.CharacterId, notification.EnemyId, notification.Outcome, ratingResult, cancellationToken);
}
}