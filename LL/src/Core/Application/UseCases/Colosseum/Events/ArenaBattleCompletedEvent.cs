using Domain.Models.Combat;
using MediatR;

namespace Application.UseCases.Colosseum.Events;
public record ArenaBattleCompletedEvent(Guid CharacterId, Guid EnemyId, BattleOutcome Outcome) : INotification;