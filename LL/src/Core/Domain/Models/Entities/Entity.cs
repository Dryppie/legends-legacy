using System.ComponentModel.DataAnnotations.Schema;
using Domain.Models.Abilities;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Essences.EssenceSlots;
using Domain.Models.Items.Equipments;

namespace Domain.Models.Entities;
public abstract class Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<EntityAttribute> BaseAttributes { get; set; } = [];
    public ICollection<EssenceSlot> EssenceSlots { get; set; } = [];
    [NotMapped]
    public List<string> AbilityIds { get; set; } = [];
    [NotMapped]
    public List<AbilityDefinition> Abilities { get; set; } = [];
    [NotMapped]
    public int NextBasicAttackIn = 300; // TODO: Turn 300 into a Constant somewhere, as it is also stored in the CombatSimulator class
                                        // Every tick, this decrements by BaseAttackSpeed.
                                        // Start at 300. Whenever it is equal to or lower than 0, perform the attack.
                                        // If you increase attack speed by 100%, BasicAttackSpeed goes from 10 to 20,
                                        // and thust counting down faster to the next attack each tick
    public bool IsAlive => CombatAttributes.FirstOrDefault(cm => cm.Key.Equals(AttributeType.Health)).Value > 0;
    [NotMapped]
    public List<Equipment> Equipment { get; set; } = [];
    [NotMapped]
    public Dictionary<AttributeType, float> BaseCombatAttributes { get; } = [];
    [NotMapped]
    public Dictionary<AttributeType, float> CombatAttributes { get; } = [];
    [NotMapped]
    public List<AttributeModifier> TemporaryModifiers { get; set; } = [];
    [NotMapped]
    public HashSet<string> Statuses { get; } = [];
    public int Level { get; set; } = 1;
    [NotMapped]
    public bool IsSummoned { get; set; } = false;
    public string ImagePath { get; set; } = string.Empty;

    //public Actor DeepClone()
    //{
    //    // Create a new Actor instance
    //    var clone = new Actor
    //    {
    //        Level = this.Level,
    //        RawAttributes = new List<EntityAttribute>(this.RawAttributes.Select(attr => attr.Clone())), // Assuming EntityAttribute has a DeepClone method
    //        HP = HP,
    //        // Do not copy the AttackTimer directly as it is tied to specific event handlers, create a new one instead
    //        _modifiersHaveChanged = _modifiersHaveChanged,
    //        // _attributes are recalculated, so we don't clone them directly but let them be lazy-initialized
    //    };

    //    // Copy the AttackTimer's Elapsed event handlers, if necessary
    //    // Note: This could introduce side effects as the handlers are shared between the original and the clone
    //    // foreach (ElapsedEventHandler handler in this.AttackTimer.Elapsed.GetInvocationList())
    //    // {
    //    //     clone.AttackTimer.Elapsed += handler;
    //    // }

    //    return clone;
    //}
}