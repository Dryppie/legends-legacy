using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Entities.Actors;
public class Actor : Entity
{
    //public int Level { get; set; } = 1;
    //public ICollection<EntityAttribute> RawAttributes { get; set; }


    //private ICollection<EntityAttribute> _attributes;
    //private readonly object _attributeLock = new object();

    //[NotMapped]
    //public ICollection<EntityAttribute> Attributes
    //{
    //    get
    //    {
    //        lock (_attributeLock)
    //        {
    //            if (_attributes == null || _modifiersHaveChanged)
    //            {
    //                _attributes = AttributeCalculator.CalculateAttributes(this);
    //                _modifiersHaveChanged = false;
    //            }
    //            return _attributes;
    //        }
    //    }
    //    private set
    //    {
    //        lock (_attributeLock)
    //        {
    //            _attributes = value;
    //        }
    //    }
    //}

    //[NotMapped]
    //public Timer AttackTimer { get; set; }
    //[NotMapped]
    //public float HP { get; set; }
    //[NotMapped]
    //public bool IsAlive => HP > 0;

    //public Actor()
    //{
    //    AttackTimer = new Timer(3000);
    //    AttackTimer.Elapsed += Attack;
    //    HP = 10;
    //}
    //private DateTime time = DateTime.MinValue;
    //public void Attack(object sender, ElapsedEventArgs e)
    //{
    //    if (time == DateTime.MinValue)
    //    {
    //        time = DateTime.Now;
    //    }
    //    DateTime now = DateTime.Now;
    //    Console.WriteLine(now - time);
    //}

    //private bool _modifiersHaveChanged;

    //// Call this whenever modifiers are added or removed from the Modifiers list
    //public void OnModifierChanged(Modifier modifier)
    //{
    //    CalculateAttributeByName(modifier.AttributeName);
    //    _modifiersHaveChanged = true;
    //}
    //private void CalculateAttributeByName(string attributeName) => AttributeCalculator.CalculateAttributeByName(this, attributeName);

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