using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Damages;

namespace Domain.Models.Abilities.Effects;
public class EffectContext
{
    public Effect Effect { get; set; }
    public List<CombatEntity> OwnTeam { get; set; } = [];
    public List<CombatEntity> EnemyTeam { get; set; } = [];
    /// <summary>
    /// The entity initiating this context
    /// </summary>
    public CombatEntity Actor { get; set; }
    /// <summary>
    /// The entity this effect affects when Executed
    /// </summary>
    public CombatEntity Target { get; set; }
    public AttackType AttackType { get; set; } = AttackType.None;

    /// <summary>
    /// Whether the effect in context hits, crits, is dodged, and so on.
    /// </summary>
    public AttackOutcome AttackOutcome { get; set; }
    /// <summary>
    /// How much an effect heals, damages, and so on
    /// </summary>
    public int Magnitude { get; set; }
    public string Details { get; set; } = string.Empty;
    /// <summary>
    /// This is being set in each EffectAction during execution
    /// </summary>
    public EventType EventType { get; set; }
    /// <summary>
    /// This depends on what attribute is being affected. Usually it's Health, since that's damage / healing
    /// </summary>
    public AttributeType Attribute { get; set; }

    public EffectContext(Effect effect,
                         List<CombatEntity> ownTeam,
                         List<CombatEntity> enemyTeam,
                         CombatEntity actor,
                         CombatEntity target,
                         int magnitude,
                         string details)
    {
        Effect = effect;
        OwnTeam = ownTeam;
        EnemyTeam = enemyTeam;
        Actor = actor;
        Target = target;
        Magnitude = magnitude;
        Details = details;
    }
}