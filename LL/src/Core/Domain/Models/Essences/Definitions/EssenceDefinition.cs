using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.Items;
using System.Text.Json.Serialization;

namespace Domain.Models.Essences.Definitions;

public sealed class EssenceDefinition
{
    public string Id { get; set; } = string.Empty;
    public string SourceMonsterId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Rarity Rarity { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<EssenceAttributeBonusDefinition> AttributeBonuses { get; set; } = [];
    public string ActiveAbilityId { get; set; } = string.Empty;
    public string PassiveAbilityId { get; set; } = string.Empty;
    [JsonIgnore]
    public AbilitySpec ActiveAbility { get; set; } = new();
    [JsonIgnore]
    public AbilitySpec PassiveAbility { get; set; } = new();
    public EssenceAscensionDefinition Ascension { get; set; } = new();
    public EssenceEvolutionDefinition Evolution { get; set; } = new();
}
