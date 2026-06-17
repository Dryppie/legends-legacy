using System.Text.Json.Serialization;

namespace Domain.Models.Items.Equipments.Tools;

public class ToolBonusModifier
{
    public Guid Id { get; set; }
    public string EquipmentBaseId { get; set; } = string.Empty;
    [JsonIgnore]
    public EquipmentBase? EquipmentBase { get; set; }
    public ToolBonusType BonusType { get; set; }
    public double Amount { get; set; }
    public string? ScopeId { get; set; }
}
