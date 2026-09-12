using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Raids;
using Domain.Models.RegionBosses;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.CombatStyles;

public sealed class CombatStyleActivityRepository(IDbContext db) : ICombatStyleActivityRepository
{
    public async Task<string?> GetCommittedActivityAsync(Guid characterId, CancellationToken ct)
    {
        if (await db.DungeonRuns.AnyAsync(x => x.CharacterId == characterId && x.Status == DungeonRunStatus.Active, ct))
            return "dungeon run";
        if (await db.RaidSignups.AnyAsync(x => x.CharacterId == characterId &&
                (x.RaidRun.Status == RaidRunStatus.Resolving || x.RaidRun.Status == RaidRunStatus.Playback), ct))
            return "raid battle";
        if (await db.RegionBossSignups.AnyAsync(x => x.CharacterId == characterId &&
                (x.Event.Status == RegionBossEventStatus.Matching || x.Event.Status == RegionBossEventStatus.Resolving ||
                 x.Event.Status == RegionBossEventStatus.Playback), ct))
            return "region boss battle";
        if (await db.TowerRallyParticipants.AnyAsync(x => x.CharacterId == characterId &&
                x.TowerRally.Status == TowerRallyStatus.InProgress, ct))
            return "World Tower battle";
        // Tournament combat uses loadout snapshots captured at start, so participation
        // must not prevent players from editing their live equipment or essence builds.
        return null;
    }
}
