using Domain.Models.Abilities;
using Domain.Models.Entities;
using Domain.Models.Items;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Essences;
public class Essence
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PassiveAbilityId { get; set; } = string.Empty;
    public string ActiveAbilityId { get; set; } = string.Empty;
    [NotMapped]
    public Ability PassiveAbility { get; set; } = null!;
    [NotMapped]
    public Ability ActiveAbility { get; set; } = null!;

    public ICollection<Entity> Entities { get; set; } = [];
    public ICollection<EssenceItem> EssenceItems { get; set; } = new List<EssenceItem>();
}