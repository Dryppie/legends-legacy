using Domain.Models.LootTables;

namespace Domain.Models.Combat;
public class CombatAction
{
    public List<Guid> CharacterTeam = [];
    public List<Guid> EnemyTeam = [];
    public LootTable? LootTable = new();

    public CombatAction(List<Guid> characterTeam, List<Guid> enemyTeam, LootTable? lootTable = null)
    {
        CharacterTeam = characterTeam;
        EnemyTeam = enemyTeam;
        LootTable = lootTable;
    }
}