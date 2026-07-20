using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Dungeons.Definitions.Rooms;

namespace Domain.Models.Dungeons.Definitions;

public sealed class DungeonDelveDefinition
{
    public string Id { get; set; } = string.Empty;
    public List<string> DungeonDefinitionIds { get; set; } = [];
    public List<DungeonDelveNodeDefinition> Nodes { get; set; } = [];
    public List<DungeonDelveOmenDefinition> Omens { get; set; } = [];
    public List<DungeonDelveAspectDefinition> BossAspects { get; set; } = [];
}

public sealed class DungeonDelveNodeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int Depth { get; set; }
    public int Lane { get; set; }
    public int Section { get; set; }
    public List<int> NextRoomIndexes { get; set; } = [];
    public string Forecast { get; set; } = string.Empty;
    public int VigorCostMin { get; set; }
    public int VigorCostMax { get; set; }
    public string BossConsequence { get; set; } = string.Empty;
    public string BossAspectId { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
}

public sealed class DungeonDelveOmenDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CombatTollModifier { get; set; }
    public int HazardTollModifier { get; set; }
}

public sealed class DungeonDelveAspectDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public AttributeType AttributeType { get; set; }
    public float Amount { get; set; }
    public ModifierType ModifierType { get; set; } = ModifierType.Additive;
    public int MinimumTier { get; set; } = 1;
}
