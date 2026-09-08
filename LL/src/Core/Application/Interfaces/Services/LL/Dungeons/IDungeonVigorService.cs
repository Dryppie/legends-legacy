using Domain.Models.Combat;
using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonVigorService
{
    int ApplyCombatToll(DungeonRun run, RoomInstance room, CombatResult result);
    int RecoverAtRestSite(DungeonRun run, RoomInstance room);
    int SpendTreasuryVigor(DungeonRun run, RoomInstance room, int cost);
    void RefreshState(DungeonRun run);
}
