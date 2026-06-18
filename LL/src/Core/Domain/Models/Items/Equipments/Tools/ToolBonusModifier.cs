using System.Text.Json.Serialization;
using Domain.Models.Items.Equipments;

namespace Domain.Models.Items.Equipments.Tools;

public class ToolBonusModifier
{
    public Guid Id { get; set; }
    public string? EquipmentBaseId { get; set; }
    [JsonIgnore]
    public EquipmentBase? EquipmentBase { get; set; }
    [JsonIgnore]
    public Guid? EquipmentInstanceId { get; set; }
    [JsonIgnore]
    public EquipmentInstance? EquipmentInstance { get; set; }
    public string? Name { get; set; }
    public ToolBonusType BonusType { get; set; }
    public double Amount { get; set; }
    public string? ScopeId { get; set; }
}
