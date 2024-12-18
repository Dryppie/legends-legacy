using Domain.Interfaces;
using Domain.Models.Abilities.Effects.EffectModifications;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Combat;
using Domain.Models.Entities;

namespace Domain.Models.Abilities.Effects;
public class EffectContext
{
    public List<Entity> OwnTeam { get; set; } = [];
    public List<Entity> EnemyTeam { get; set; } = [];
    /// <summary>
    /// The Caster, or the Entity with this effect in their ActiveEffects list
    /// </summary>
    public Entity Owner { get; set; }
    /// <summary>
    /// Whomever the effect is going to affect
    /// </summary>
    public Entity Target { get; set; }
    /// <summary>
    /// What trigger caused this effect to take place
    /// </summary>
    public TriggerEvent TriggerEvent { get; set; }
    /// <summary>
    /// Whether the effect in context hits, crits, is dodged, and so on.
    /// </summary>
    public AttackOutcome AttackOutcome { get; set; }
    /// <summary>
    /// How much an effect heals, damages, and so on
    /// </summary>
    public int Magnitude { get; set; }
    /// <summary>
    /// Primarily used for stuff such as DOTs
    /// </summary>
    public bool IsFlatAmount { get; set; }
    public int TimeStamp { get; set; }
    public string Details { get; set; } = string.Empty;
    /// <summary>
    /// What type of Effect Action this context about
    /// </summary>
    public IEffectAction Action { get; set; }
    /// <summary>
    /// This is being set in each EffectAction during execution
    /// </summary>
    public EventType EventType { get; set; }
    /// <summary>
    /// All modifications being done to an effect in this context
    /// </summary>
    public List<EffectModification> EffectModifications { get; set; } = [];

    public EffectContext(List<Entity> ownTeam,
                         List<Entity> enemyTeam,
                         Entity owner,
                         Entity target,
                         TriggerEvent triggerEvent,
                         int magnitude,
                         bool isFlatAmount,
                         string details,
                         List<EffectModification> effectModifications,
                         IEffectAction action)
    {
        OwnTeam = ownTeam;
        EnemyTeam = enemyTeam;
        Owner = owner;
        Target = target;
        TriggerEvent = triggerEvent;
        Magnitude = magnitude;
        IsFlatAmount = isFlatAmount;
        Details = details;
        EffectModifications = effectModifications;
        Action = action;
    }
}