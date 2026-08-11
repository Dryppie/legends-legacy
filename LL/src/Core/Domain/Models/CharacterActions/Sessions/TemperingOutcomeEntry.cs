using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting;

namespace Domain.Models.CharacterActions.Sessions;

public sealed class TemperingOutcomeEntry
{
    public Guid Id { get; set; }
    public Guid QueueItemId { get; set; }
    public Guid EquipmentInstanceId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public TemperingOutcome Outcome { get; set; }
    public int PotentialSpent { get; set; }
    public int PreviousPotential { get; set; }
    public int NewPotential { get; set; }
    public int PreviousItemXp { get; set; }
    public int NewItemXp { get; set; }
    public bool BecameMasterpiece { get; set; }
    public bool BecameLevelingItem { get; set; }
    public Rarity PreviousRarity { get; set; }
    public Rarity NewRarity { get; set; }
    public bool RarityUpgraded { get; set; }
    public bool QualityIncreased { get; set; }
    public ItemQuality? PreviousQuality { get; set; }
    public ItemQuality? NewQuality { get; set; }
    public AttributeType? ImprovedStat { get; set; }
    public float? PreviousStatValue { get; set; }
    public float? NewStatValue { get; set; }
}
