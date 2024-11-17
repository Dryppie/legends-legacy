namespace Domain.Models.Items;
public class EssenceItem : Item
{
    public string PassiveAbilityId { get; set; } = string.Empty;
    public string ActiveAbilityId {  get; set; } = string.Empty;
}
