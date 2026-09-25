using System.Text.Json;
using Domain.Models.Analytics;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Analytics;

public sealed class TelemetryRepository(LLDbContext db) : ITelemetryRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task RecordActivityAsync(Guid accountId, DateTimeOffset seenAtUtc, CancellationToken ct)
    {
        var day = DateOnly.FromDateTime(seenAtUtc.UtcDateTime);
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"AccountActivityDays\" (\"AccountId\", \"ActivityDateUtc\", \"FirstSeenAtUtc\") VALUES ({accountId}, {day}, {seenAtUtc}) ON CONFLICT (\"AccountId\", \"ActivityDateUtc\") DO NOTHING", ct);
    }

    public async Task GenerateDailyReportsAsync(DateOnly yesterdayUtc, CancellationToken ct)
    {
        // Finalization can arrive after a run starts. Recompute bounded, durable outcomes.
        for (var offset = 6; offset >= 0; offset--)
        {
            var day = yesterdayUtc.AddDays(-offset);
            var existing = await db.DailyTelemetryReports.FindAsync([day], ct);
            var previous = existing is null ? null : JsonSerializer.Deserialize<TelemetrySnapshot>(existing.PayloadJson, JsonOptions);
            var population = offset == 0 || previous is null
                ? await PopulationAsync(day, ct) : previous.Population;
            var outcomes = await OutcomesAsync(day, ct);
            var captureSnapshot = previous is null;
            var adoption = captureSnapshot ? await AdoptionAsync(day, ct) : previous!.Adoption;
            var economy = captureSnapshot ? await EconomyAsync(day, ct) : previous!.Economy;
            var report = new TelemetrySnapshot(day, DateTimeOffset.UtcNow,
                captureSnapshot ? DateTimeOffset.UtcNow : previous!.SnapshotAtUtc,
                population, outcomes, adoption, economy);
            if (existing is null)
            {
                db.DailyTelemetryReports.Add(new DailyTelemetryReport
                {
                    ReportDateUtc = day,
                    GeneratedAtUtc = report.GeneratedAtUtc,
                    PayloadJson = JsonSerializer.Serialize(report, JsonOptions)
                });
            }
            else
            {
                existing.GeneratedAtUtc = report.GeneratedAtUtc;
                existing.PayloadJson = JsonSerializer.Serialize(report, JsonOptions);
            }
            await db.SaveChangesAsync(ct);
        }

        var expiredBefore = yesterdayUtc.AddMonths(-13);
        await db.AccountActivityDays.Where(x => x.ActivityDateUtc < expiredBefore).ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<TelemetrySnapshot>> GetReportsAsync(int days, CancellationToken ct)
    {
        var since = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-Math.Clamp(days, 1, 90));
        var rows = await db.DailyTelemetryReports.AsNoTracking()
            .Where(x => x.ReportDateUtc >= since)
            .OrderByDescending(x => x.ReportDateUtc)
            .Select(x => x.PayloadJson)
            .ToListAsync(ct);
        return rows.Select(x => JsonSerializer.Deserialize<TelemetrySnapshot>(x, JsonOptions)!)
            .ToArray();
    }

    private async Task<PopulationMetrics> PopulationAsync(DateOnly day, CancellationToken ct)
    {
        var start = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);
        var dau = await db.AccountActivityDays.CountAsync(x => x.ActivityDateUtc == day, ct);
        var wau = await db.AccountActivityDays.Where(x => x.ActivityDateUtc > day.AddDays(-7) && x.ActivityDateUtc <= day)
            .Select(x => x.AccountId).Distinct().CountAsync(ct);
        var mau = await db.AccountActivityDays.Where(x => x.ActivityDateUtc > day.AddDays(-30) && x.ActivityDateUtc <= day)
            .Select(x => x.AccountId).Distinct().CountAsync(ct);
        var newActive = await (from activity in db.AccountActivityDays
            join user in db.Users on activity.AccountId equals user.Id
            where activity.ActivityDateUtc == day && user.CreatedUtc >= start && user.CreatedUtc < end
            select activity.AccountId).CountAsync(ct);
        var d1 = await ReturnCohortAsync(day.AddDays(-1), day, ct);
        var d7 = await ReturnCohortAsync(day.AddDays(-7), day, ct);
        return new PopulationMetrics(dau, wau, mau, newActive, dau - newActive,
            d1.Cohort, d1.Returned, d7.Cohort, d7.Returned);
    }

    private async Task<(int Cohort, int Returned)> ReturnCohortAsync(DateOnly created,
        DateOnly returned, CancellationToken ct)
    {
        var start = created.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);
        var cohort = await db.Users.CountAsync(x => x.CreatedUtc >= start && x.CreatedUtc < end, ct);
        var count = await (from user in db.Users
            join activity in db.AccountActivityDays on user.Id equals activity.AccountId
            where user.CreatedUtc >= start && user.CreatedUtc < end && activity.ActivityDateUtc == returned
            select user.Id).CountAsync(ct);
        return (cohort, count);
    }

    private sealed record OutcomeEvent(string Kind, string Key, Guid CharacterId,
        bool Started, bool Completed, bool Failed);

    private async Task<IReadOnlyList<ContentOutcomeMetric>> OutcomesAsync(DateOnly day, CancellationToken ct)
    {
        var start = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var end = start.AddDays(1);
        var events = new List<OutcomeEvent>();

        var dungeons = await db.DungeonAttemptHistories.AsNoTracking()
            .Where(x => x.StartedAtUtc >= start && x.StartedAtUtc < end ||
                x.FinishedAtUtc >= start && x.FinishedAtUtc < end)
            .Select(x => new { x.CharacterId, x.DungeonDefinitionId, x.StartedAtUtc, x.FinishedAtUtc, x.Outcome })
            .ToListAsync(ct);
        foreach (var x in dungeons)
            events.Add(new OutcomeEvent("dungeon", x.DungeonDefinitionId, x.CharacterId,
                x.StartedAtUtc >= start && x.StartedAtUtc < end,
                x.FinishedAtUtc >= start && x.FinishedAtUtc < end && x.Outcome == "Completed",
                x.FinishedAtUtc >= start && x.FinishedAtUtc < end && x.Outcome is "Failed" or "Retreated"));

        var matches = await db.ColosseumMatches.AsNoTracking()
            .Where(x => x.PlayedAt >= start && x.PlayedAt < end)
            .Select(x => new { x.CharacterAId, x.WinnerId, x.CharacterARatingBefore })
            .ToListAsync(ct);
        foreach (var x in matches)
            events.Add(new OutcomeEvent("colosseum", $"rating-{x.CharacterARatingBefore / 500 * 500}+",
                x.CharacterAId, true, true, x.WinnerId.HasValue && x.WinnerId != x.CharacterAId));

        var tower = await (from attempt in db.TowerAttempts.AsNoTracking()
            join participant in db.TowerRallyParticipants.AsNoTracking() on attempt.TowerRallyId equals participant.TowerRallyId
            where attempt.StartedAt >= start && attempt.StartedAt < end ||
                attempt.CompletedAt >= start && attempt.CompletedAt < end
            select new { participant.CharacterId, attempt.FloorNumber, attempt.StartedAt,
                attempt.CompletedAt, attempt.Status }).ToListAsync(ct);
        foreach (var x in tower)
            events.Add(new OutcomeEvent("tower", x.FloorNumber.ToString(), x.CharacterId,
                x.StartedAt >= start && x.StartedAt < end,
                x.CompletedAt >= start && x.CompletedAt < end && x.Status == TowerAttemptStatus.Succeeded,
                x.CompletedAt >= start && x.CompletedAt < end && x.Status == TowerAttemptStatus.Failed));

        var raids = await (from run in db.RaidRuns.AsNoTracking()
            join signup in db.RaidSignups.AsNoTracking().Where(x => x.Status == Domain.Models.Raids.RaidSignupStatus.Approved)
                on run.Id equals signup.RaidRunId
            where run.CommencedAt >= start && run.CommencedAt < end ||
                run.ResolvedAt >= start && run.ResolvedAt < end
            select new { signup.CharacterId, run.RaidBossId, run.CommencedAt, run.ResolvedAt, run.Outcome })
            .ToListAsync(ct);
        foreach (var x in raids)
            events.Add(new OutcomeEvent("raid", x.RaidBossId, x.CharacterId,
                x.CommencedAt >= start && x.CommencedAt < end,
                x.ResolvedAt >= start && x.ResolvedAt < end && x.Outcome != null && x.Outcome != Domain.Models.Raids.RaidOutcome.Repelled,
                x.ResolvedAt >= start && x.ResolvedAt < end && x.Outcome == Domain.Models.Raids.RaidOutcome.Repelled));

        var region = await (from run in db.RegionBossRuns.AsNoTracking()
            join signup in db.RegionBossSignups.AsNoTracking() on run.Id equals signup.RegionBossRunId
            join bossEvent in db.RegionBossEvents.AsNoTracking() on run.RegionBossEventId equals bossEvent.Id
            where run.StartedAtUtc >= start && run.StartedAtUtc < end ||
                run.ResolvedAtUtc >= start && run.ResolvedAtUtc < end
            select new { signup.CharacterId, bossEvent.RegionBossDefinitionId, run.StartedAtUtc,
                run.ResolvedAtUtc, run.HighestLevelDefeated }).ToListAsync(ct);
        foreach (var x in region)
            events.Add(new OutcomeEvent("region-boss", x.RegionBossDefinitionId, x.CharacterId,
                x.StartedAtUtc >= start && x.StartedAtUtc < end,
                x.ResolvedAtUtc >= start && x.ResolvedAtUtc < end && x.HighestLevelDefeated > 0,
                x.ResolvedAtUtc >= start && x.ResolvedAtUtc < end && x.HighestLevelDefeated == 0));

        return events.GroupBy(x => (x.Kind, x.Key))
            .Select(g => new ContentOutcomeMetric(g.Key.Kind, g.Key.Key,
                g.Count(x => x.Started), g.Count(x => x.Completed), g.Count(x => x.Failed),
                g.Select(x => x.CharacterId).Distinct().Count()))
            .OrderBy(x => x.Kind).ThenBy(x => x.Key).ToArray();
    }

    private async Task<IReadOnlyList<AdoptionMetric>> AdoptionAsync(DateOnly day, CancellationToken ct)
    {
        var result = new List<AdoptionMetric>();
        foreach (var window in new[] { 7, 30 })
        {
            var activeIds = db.AccountActivityDays.AsNoTracking()
                .Where(x => x.ActivityDateUtc > day.AddDays(-window) && x.ActivityDateUtc <= day)
                .Select(x => x.AccountId).Distinct();
            var characters = await db.Characters.AsNoTracking()
                .Where(x => activeIds.Contains(x.UserId))
                .Select(x => new { x.Id, x.Level }).ToListAsync(ct);
            var ids = characters.Select(x => x.Id).ToArray();
            if (ids.Length == 0) continue;
            var bands = characters.ToDictionary(x => x.Id, x => LevelBand(x.Level));
            var denominators = characters.GroupBy(x => LevelBand(x.Level))
                .ToDictionary(x => x.Key, x => x.Count());
            var owned = await db.PlayerEssences.AsNoTracking()
                .Where(x => ids.Contains(x.CharacterId))
                .Select(x => new { x.CharacterId, x.EssenceDefinitionId }).Distinct().ToListAsync(ct);
            foreach (var group in owned.GroupBy(x => (x.EssenceDefinitionId, Band: bands[x.CharacterId])))
                result.Add(new AdoptionMetric(window, "essence-owned", group.Key.EssenceDefinitionId,
                    group.Key.Band, denominators[group.Key.Band], group.Select(x => x.CharacterId).Distinct().Count()));
            var saved = await (from slot in db.EssenceLoadoutSlots.AsNoTracking()
                join loadout in db.EssenceLoadouts.AsNoTracking() on slot.EssenceLoadoutId equals loadout.Id
                join essence in db.PlayerEssences.AsNoTracking() on slot.PlayerEssenceId equals essence.Id
                where ids.Contains(loadout.CharacterId) && loadout.PresetSlot <= 3
                select new { loadout.CharacterId, essence.EssenceDefinitionId }).Distinct().ToListAsync(ct);
            foreach (var group in saved.GroupBy(x => (x.EssenceDefinitionId, Band: bands[x.CharacterId])))
                result.Add(new AdoptionMetric(window, "essence-saved", group.Key.EssenceDefinitionId,
                    group.Key.Band, denominators[group.Key.Band], group.Select(x => x.CharacterId).Distinct().Count()));
            var selected = await db.CharacterCombatStyleSelections.AsNoTracking()
                .Where(x => ids.Contains(x.CharacterId) && x.CombatStyleId != null)
                .Select(x => new { x.CharacterId, x.CombatStyleId }).ToListAsync(ct);
            foreach (var group in selected.GroupBy(x => (Style: x.CombatStyleId!, Band: bands[x.CharacterId])))
                result.Add(new AdoptionMetric(window, "style-selected", group.Key.Style,
                    group.Key.Band, denominators[group.Key.Band], group.Select(x => x.CharacterId).Distinct().Count()));
        }
        return result;
    }

    private async Task<IReadOnlyList<EconomyMetric>> EconomyAsync(DateOnly day, CancellationToken ct)
    {
        var result = new List<EconomyMetric>();
        foreach (var window in new[] { 7, 30 })
        {
            var activeIds = db.AccountActivityDays.AsNoTracking()
                .Where(x => x.ActivityDateUtc > day.AddDays(-window) && x.ActivityDateUtc <= day)
                .Select(x => x.AccountId).Distinct();
            var balances = await db.Characters.AsNoTracking().Where(x => activeIds.Contains(x.UserId))
                .Select(x => new { x.Level, x.Cinders, x.Soulstones }).ToListAsync(ct);
            foreach (var group in balances.GroupBy(x => LevelBand(x.Level)))
            {
                result.Add(Distribution(window, "cinders", group.Key, group.Select(x => x.Cinders)));
                result.Add(Distribution(window, "soulstones", group.Key, group.Select(x => x.Soulstones)));
            }
        }
        return result;
    }

    private static EconomyMetric Distribution(int window, string resource, string band, IEnumerable<long> values)
    {
        var sorted = values.Order().ToArray();
        return new EconomyMetric(window, resource, band, sorted.Length,
            sorted.Count(x => x == 0), sorted[(sorted.Length - 1) / 2],
            sorted[(int)Math.Ceiling(sorted.Length * .9) - 1]);
    }

    private static string LevelBand(int level) => level < 20 ? "1-19" : level < 50 ? "20-49" : "50+";
}
