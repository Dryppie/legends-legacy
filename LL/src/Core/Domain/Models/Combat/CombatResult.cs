using Domain.Models.Inventories;

using System.Text.Json.Serialization;

namespace Domain.Models.Combat;
public class CombatResult
{
    private BattleOutcome _engineOutcome;
    private BattleOutcome _contentOutcome;
    private bool _hasExplicitEngineOutcome;
    private bool _hasExplicitContentOutcome;

    public List<SimpleCombatEntity> PlayerTeam { get; set; } = [];
    public List<SimpleCombatEntity> EnemyTeam { get; set; } = [];
    public List<CombatLogItem> EventLog { get; set; } = [];
    public List<EntityStats> EntityStats { get; set; } = [];
    public BattleOutcome Outcome
    {
        get => ContentOutcome;
        init
        {
            if (!_hasExplicitEngineOutcome)
                _engineOutcome = value;
            if (!_hasExplicitContentOutcome)
                _contentOutcome = value;
        }
    }

    [JsonInclude]
    public BattleOutcome EngineOutcome
    {
        get => _engineOutcome;
        private init
        {
            _engineOutcome = value;
            _hasExplicitEngineOutcome = true;
        }
    }

    [JsonInclude]
    public BattleOutcome ContentOutcome
    {
        get => _contentOutcome;
        private init
        {
            _contentOutcome = value;
            _hasExplicitContentOutcome = true;
        }
    }
    public List<InventoryItem> Loot { get; set; } = [];
    public List<GatheringRewardResult> GatheringRewards { get; set; } = [];
    public int ExperienceGained { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public int Duration { get; set; }

    public void ApplyContentOutcome(BattleOutcome outcome) => _contentOutcome = outcome;
}
