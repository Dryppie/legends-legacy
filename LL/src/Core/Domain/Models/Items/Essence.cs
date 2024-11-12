namespace Domain.Models.Items;
public class Essence : Item
{
    public string EssenceName { get; set; } = string.Empty;
    public string PassiveAbilityId { get; set; } = string.Empty;
    public string ActiveAbilityId {  get; set; } = string.Empty;
}
