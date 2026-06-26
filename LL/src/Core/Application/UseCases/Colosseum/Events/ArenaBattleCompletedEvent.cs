using Domain.Models.Combat;
using MediatR;

namespace Application.UseCases.Colosseum.Events;
public record ArenaBattleCompletedEvent(
    Guid CharacterId,
    Guid EnemyId,
    BattleOutcome Outcome,
    int CharacterRatingBefore,
    int CharacterRatingAfter,
    int EnemyRatingBefore,
    int EnemyRatingAfter) : INotification;
