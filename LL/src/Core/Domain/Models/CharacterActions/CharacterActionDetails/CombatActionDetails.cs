namespace Domain.Models.CharacterActions.CharacterActionDetails;
public class CombatActionDetails : ActionDetails
{
    public List<Guid> CharacterTeam { get; set; } = [];
    public List<Guid> EnemyTeam { get; set; } = [];

    public CombatActionDetails(List<Guid> characterTeam, List<Guid> enemyTeam)
    {
        CharacterTeam = characterTeam;
        EnemyTeam = enemyTeam;
    }
    public CombatActionDetails()
    {

    }
}
