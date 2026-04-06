using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Encounters;
using Domain.Models.Dungeons.Definitions.Rooms;

namespace Domain.Models.Dungeons;

public sealed class DungeonDefinition
{
    public string Id { get; set; } = default!;         // e.g. "crypt_of_thorns"
    public string Name { get; set; } = default!;
    public string SigilItemId { get; set; } = default!;
    public bool HasCheckpoint { get; set; } = true;
    public int MinRooms { get; set; }
    public int MaxRooms { get; set; }
    public List<RoomDefinition> Rooms { get; set; } = [];
}
