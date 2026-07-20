using Domain.Models.Combat;
using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonVigorService
{
    int ApplyCombatToll(DungeonRun run, RoomInstance room, CombatResult result);
    int ApplyHazardToll(DungeonRun run, RoomInstance room, int baseToll);
    int ApplyEventChange(DungeonRun run, RoomInstance room, int amount, string reason);
    int RecoverAtWardstone(DungeonRun run, RoomInstance room);
    void RefreshState(DungeonRun run);
}
