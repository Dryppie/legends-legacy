using Domain.Models.Entities.Characters;

namespace Domain.Models.Colosseum;

public sealed class CharacterArenaProfile
{
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public int Rating { get; set; } = 1000;
    public int LifetimeHighestRating { get; set; } = 1000;
    public int Glory { get; set; }
    public int CurrentAttackWinStreak { get; set; }
    public int BestAttackWinStreak { get; set; }
    public int AttackWins { get; set; }
    public int AttackDraws { get; set; }
    public int AttackLosses { get; set; }
    public int DefenseWins { get; set; }
    public int DefenseDraws { get; set; }
    public int DefenseLosses { get; set; }
    public DateTimeOffset? LastFirstWinBonusAt { get; set; }
}
