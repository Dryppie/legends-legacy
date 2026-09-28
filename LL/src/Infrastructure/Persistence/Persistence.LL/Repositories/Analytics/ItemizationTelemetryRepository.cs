using System.Text.Json;
using Domain.Models.Analytics;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Analytics;

public sealed class ItemizationTelemetryRepository(LLDbContext db) : IItemizationTelemetryRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task RecordAsync(ItemizationObservation observation, CancellationToken ct)
    {
        if (observation.Id.Length != 64 || observation.CharacterId == Guid.Empty
            || observation.InclusionProbability is <= 0 or > 1)
            throw new InvalidOperationException("Invalid itemization observation.");
        var json = JsonSerializer.Serialize(observation, JsonOptions);
        // Receipt and observation are committed in the outbox delivery transaction. Replay is a no-op.
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"ItemizationObservations\" (\"Id\", \"OccurredAtUtc\", \"CharacterId\", \"Kind\", \"PayloadJson\") VALUES ({observation.Id}, {observation.OccurredAtUtc}, {observation.CharacterId}, {observation.Kind}, CAST({json} AS jsonb)) ON CONFLICT (\"Id\") DO NOTHING", ct);
    }

    public async Task GenerateDailyReportsAsync(DateOnly yesterday, CancellationToken ct)
    {
        // Recompute for late outbox deliveries; retain compact daily reports for thirteen months.
        for (var offset = 6; offset >= 0; offset--)
        {
            var day = yesterday.AddDays(-offset);
            var start = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            var end = start.AddDays(1);
            var opportunityStart = start.AddDays(-7);
            var payloads = await db.Set<ItemizationObservationRow>().AsNoTracking()
                .Where(x => x.OccurredAtUtc >= opportunityStart && x.OccurredAtUtc < end).Select(x => x.PayloadJson).ToListAsync(ct);
            var observations = payloads.Select(x => JsonSerializer.Deserialize<ItemizationObservation>(x, JsonOptions)!).ToArray();
            var report = ItemizationCohortReport.Create(day, observations.Where(x => x.OccurredAtUtc >= start)) with
                { SevenDayOpportunities = ItemizationOpportunity.Create(day, observations) };
            var row = await db.Set<ItemizationDailyReport>().FindAsync([day], ct);
            if (row is null) db.Set<ItemizationDailyReport>().Add(row = new() { Day = day });
            row.PayloadJson = JsonSerializer.Serialize(report, JsonOptions);
            await db.SaveChangesAsync(ct);
        }
        var cutoff = new DateTimeOffset(yesterday.AddDays(-30).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        await db.Set<ItemizationObservationRow>().Where(x => x.OccurredAtUtc < cutoff).ExecuteDeleteAsync(ct);
        await db.Set<ItemizationDailyReport>().Where(x => x.Day < yesterday.AddMonths(-13)).ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<ItemizationCohortReport>> GetReportsAsync(int days, CancellationToken ct)
    {
        var since = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-Math.Clamp(days, 1, 90));
        var rows = await db.Set<ItemizationDailyReport>().AsNoTracking().Where(x => x.Day >= since)
            .OrderByDescending(x => x.Day).Select(x => x.PayloadJson).ToListAsync(ct);
        return rows.Select(x => JsonSerializer.Deserialize<ItemizationCohortReport>(x, JsonOptions)!).ToArray();
    }
}
