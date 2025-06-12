using Domain.Models.Inventories;

namespace Domain.Models.Combat;
public class CombatResult
{
    public List<SimpleCombatEntity> PlayerTeam { get; set; } = [];
    public List<SimpleCombatEntity> EnemyTeam { get; set; } = [];
    public List<CombatLogItem> EventLog { get; set; } = [];
    public BattleOutcome Outcome { get; set; }
    public List<InventoryItem> Loot { get; set; } = [];
    public int ExperienceGained { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public int Duration { get; set; }
}