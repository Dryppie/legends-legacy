using Domain.Models.Attributes;
using Domain.Models.Entities;

namespace Domain.Models.Masteries;
public class Mastery
{
    public Guid EntityId { get; set; }
    public Entity Entity { get; set; } = null!;
    public CombatMastery MasteryType { get; set; }
    public int Level { get; set; }
    public int CurrentXP { get; set; }
    public AttributeType AttributeType { get; set; }
    public int XPThresholdForNextLevel
    {
        get { return 100 + (Level * 20); }
    }
}