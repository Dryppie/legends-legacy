namespace Domain.Models.CharacterActions.CombatActions;
public class CombatAction
{
    public Guid Id { get; set; }
    public List<Guid> CharacterTeam { get; set; } = [];
    public List<Guid> EnemyTeam { get; set; } = [];

    public CombatAction(List<Guid> characterTeam, List<Guid> enemyTeam)
    {
        CharacterTeam = characterTeam;
        EnemyTeam = enemyTeam;
    }
    public CombatAction()
    {

    }
}