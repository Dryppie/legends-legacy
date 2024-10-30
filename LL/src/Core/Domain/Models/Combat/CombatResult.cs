using Domain.Models.Inventories;

namespace Domain.Models.Combat;
public class CombatResult
{
    public List<CombatEntity> PlayerTeam { get; set; } = [];
    public List<CombatEntity> EnemyTeam { get; set; } = [];
    public List<CombatEvent> EventLog { get; set; } = [];
    public BattleOutcome Outcome { get; set; }
    public List<InventoryItem> Loot { get; set; } = [];
    public int ExperienceGained { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public int Duration { get; set; }
}