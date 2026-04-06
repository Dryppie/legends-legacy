using Domain.Models.Dungeons.Definitions.Events;
using Domain.Models.Dungeons.Definitions.Rooms;

namespace Domain.Models.Dungeons.Runs;

public sealed class RoomInstance
{
    public Guid Id { get; set; }
    public int RoomIndex { get; set; }
    public RoomType Type { get; set; }

    public RoomInstanceStatus Status { get; set; } = RoomInstanceStatus.Pending;

    // For Combat floors: the resolved encounter IDs for this run.
    public List<string> EncounterIds { get; set; } = [];

    // For Event floors: what outcome we rolled.
    public EventOutcomeType? EventOutcome { get; set; }

    // For Treasure rooms: generated options for the player to pick.
    //public TreasureRoomInstance? Treasure { get; set; }

    // For Shrine: what blessings were offered/chosen.
    //public ShrineInstance? Shrine { get; set; }

    // For Trap: parameters/effects applied.
    //public TrapInstance? Trap { get; set; }
}
