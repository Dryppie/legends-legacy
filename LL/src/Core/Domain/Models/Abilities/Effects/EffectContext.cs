using Domain.Models.Abilities.Effects.EffectModifications;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Damages;

namespace Domain.Models.Abilities.Effects;
public class EffectContext
{
    public CombatEntity Source { get; set; }
    public CombatEntity Target { get; set; }
    public AttackType AttackType { get; set; } = AttackType.None;
    public List<EffectModification> EffectModifications { get; set; } = [];
    public string Details { get; set; } = string.Empty;
    public EffectContext(CombatEntity source,
                         CombatEntity target,
                         AttackType attackType,
                         List<EffectModification> effectModifications,
                         string details)
    {
        Source = source;
        Target = target;
        AttackType = attackType;
        EffectModifications = effectModifications;
        Details = details;
    }

    /// <summary>
    /// How much an effect heals, damages, and so on
    /// </summary>
    public int Magnitude { get; set; }
    public AttackOutcome AttackOutcome { get; set; }
    /// <summary>
    /// This is being set in each EffectAction during execution
    /// </summary>
    public EventType EventType { get; set; }
    /// <summary>
    /// This depends on what attribute is being affected. Usually it's Health, since that's damage / healing
    /// </summary>
    public AttributeType Attribute { get; set; }

}