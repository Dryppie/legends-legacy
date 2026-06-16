namespace Application.WebSockets.Contracts;

public record ArenaBattleCompletedMsg(
    Guid CharacterId,
    Guid EnemyId,
    string Outcome,
    int CharacterRatingBefore,
    int CharacterRatingAfter,
    int EnemyRatingBefore,
    int EnemyRatingAfter) : GameEventMsg;
