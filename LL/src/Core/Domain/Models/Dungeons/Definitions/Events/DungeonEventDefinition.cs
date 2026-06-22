namespace Domain.Models.Dungeons.Definitions.Events;

public sealed class DungeonEventDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EventOutcomeType OutcomeType { get; set; } = EventOutcomeType.TreasureRoom;
    public List<string> DungeonDefinitionIds { get; set; } = [];
    public List<DungeonEventChoiceDefinition> Choices { get; set; } = [];
}

public sealed class DungeonEventChoiceDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PressureDelta { get; set; }
    public int RewardMultiplierDeltaPercent { get; set; }
    public List<string> RequiredFlags { get; set; } = [];
    public List<string> RequiredMissingFlags { get; set; } = [];
    public List<string> AddFlags { get; set; } = [];
    public List<string> RemoveFlags { get; set; } = [];
    public bool GrantsBoonChoice { get; set; }
    public bool GrantsLoot { get; set; }
    public int AmbushChancePercent { get; set; }
    public bool RevealsHiddenRoute { get; set; }
}
