using Domain.Models.Dungeons.Definitions.Events;
using Domain.Models.Dungeons.Definitions.Floors;

namespace Domain.Models.Dungeons.Runs;

public sealed class RunFloorState
{
    public int FloorIndex { get; set; }
    public FloorType Type { get; set; }

    public FloorProgressStatus Status { get; set; } = FloorProgressStatus.Pending;

    // For Combat floors: the resolved encounter IDs for this run.
    public List<Guid> EncounterIds { get; set; } = new();

    // For Event floors: what outcome we rolled.
    public EventOutcomeType? EventOutcome { get; set; }

    // For Treasure rooms: generated options for the player to pick.
    public TreasureRoomInstance? Treasure { get; set; }

    // For Shrine: what blessings were offered/chosen.
    public ShrineInstance? Shrine { get; set; }

    // For Trap: parameters/effects applied.
    public TrapInstance? Trap { get; set; }
}
