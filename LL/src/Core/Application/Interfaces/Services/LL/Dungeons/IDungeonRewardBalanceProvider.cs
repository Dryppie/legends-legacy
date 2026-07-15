using Domain.Models.Dungeons.Definitions.Rooms;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonRewardBalanceProvider
{
    DungeonEncounterReward GetEncounterReward(int dungeonTier, RoomType roomType);
}

public sealed record DungeonEncounterReward(int Experience, int Cinders);
