using Domain.Models.Entities;

namespace Domain.Models;
public class Essence
{
    public Guid Id { get; set; }
    public ICollection<Entity> Entities { get; set; } = [];
    public string EssenceName { get; set; } = string.Empty;
    public string PassiveAbilityId { get; set; } = string.Empty;
    public string ActiveAbilityId { get; set; } = string.Empty;
}