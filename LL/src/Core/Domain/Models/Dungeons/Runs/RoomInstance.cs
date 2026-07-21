using Domain.Models.Dungeons.Definitions.Rooms;

namespace Domain.Models.Dungeons.Runs;

public sealed class RoomInstance
{
    public Guid Id { get; set; }
    public int RoomIndex { get; set; }
    public RoomType Type { get; set; }
    public RoomInstanceStatus Status { get; set; } = RoomInstanceStatus.Pending;
    public List<string> EncounterIds { get; set; } = [];
}
