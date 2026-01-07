using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures.Templates;
using Domain.Models.Entities.Creatures.Templates.Enums;
using Domain.Models.LootTables;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Entities.Creatures;
public class Creature : Entity
{
    public CreatureArchetype Archetype { get; set; } = CreatureArchetype.Balanced;
    public DamageProfile DamageProfile { get; set; } = DamageProfile.Hybrid;
    public DefenseProfile DefenseProfile { get; set; } = DefenseProfile.Balanced;
    [NotMapped]
    public new List<EntityAttribute> BaseAttributes { get; set; } = [];
    [NotMapped]
    public Dictionary<AttributeType, float> BaseAttributesDict { get; set; } = [];
    public Guid LootTableId { get; set; }
    public LootTable LootTable { get; set; } = null!;
    public int BaseLevel { get; set; } = 1;
    public int Tier { get; set; } = 1;
    public int ExperienceReward {  get; set; }
    public ICollection<StatOverride> StatOverrides { get; set; } = [];
    //public ICollection<ElementAffinity> ElementAffinities { get; set; } = [];

}