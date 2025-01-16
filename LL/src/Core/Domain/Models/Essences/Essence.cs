using Domain.Models.Abilities;
using Domain.Models.Entities;
using Domain.Models.Items;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Models.Essences;
public class Essence
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PassiveAbilityId { get; set; } = string.Empty;
    public string ActiveAbilityId { get; set; } = string.Empty;
    [NotMapped]
    public AbilityDefinition Passive { get; set; } = null!;
    [NotMapped]
    public AbilityDefinition Active { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Entity> Entities { get; set; } = [];
    [JsonIgnore]
    public ICollection<EssenceItem> EssenceItems { get; set; } = [];
}