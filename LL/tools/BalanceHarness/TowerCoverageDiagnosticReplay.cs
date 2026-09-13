using System.Text.Json;
using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record CoverageTrialMetrics(int Seed, bool Won, decimal GuardianHealth, double DurationSeconds,
    double? FirstDeathSeconds, long RestoredHealth, long RegeneratedHealth, long FinalHealthDamage, long ActionDeniedTicks);

public static class TowerCoverageDiagnosticReplay
{
    public static CoverageTrialMetrics Metrics(TowerBattleReport report)
    {
        var b = report.Battle;
        var friendly = b.Summary.Statistics.Where(s => s.Team == "Friendly" && !s.IsSummonedEntity).ToArray();
        var first = friendly.Select(s => s.FirstDeathTick).Min();
        return new(b.Seed, report.Succeeded, report.GuardianHealthRemainingPercent, b.Summary.DurationSeconds,
            first / (double?)b.TicksPerSecond, friendly.Sum(s => (long)s.HealingReceived), friendly.Sum(s => (long)s.HealthRegenerated),
            friendly.Sum(s => (long)s.FinalHealthDamage), friendly.Sum(s => (long)s.ActionDeniedTicks));
    }

    public static void VerifyReplay(TowerBattleReport saved, TowerBattleReport replay)
    {
        if (replay.Battle.EventLog is not { Count: > 0 } || replay.Battle.TicksPerSecond <= 0
            || TowerCompactBundle.ReportHash(replay with { Battle = replay.Battle with { EventLog = null } }) != TowerCompactBundle.ReportHash(saved))
            throw new InvalidDataException("Detailed replay differs from its verified saved trial or has no detail.");
    }

    public static object Analyze(TowerBattleReport report, TowerBossInventoryReport inventory, IReadOnlyList<CoverageRoute> routes, int windowSeconds)
    {
        var b = report.Battle;
        if (b.EventLog is null) return new { Status = "MissingDetailedEvidence" };
        if (windowSeconds is < 1 or > 60 || b.TicksPerSecond <= 0) throw new InvalidDataException("Invalid diagnostic time window.");
        var events = b.EventLog;
        var participants = b.PreparedParticipants.EnumerateArray().ToArray();
        var friendly = participants.Where(p => p.GetProperty("slot").GetProperty("side").GetString() == "Friendly")
            .ToDictionary(p => p.GetProperty("slot").GetProperty("slotId").GetString()!, StringComparer.Ordinal);
        var stats = b.Summary.Statistics.Where(s => friendly.ContainsKey(s.EntityId)).ToArray();
        if (stats.Select(s => s.EntityId).Distinct().Count() != friendly.Count || stats.Length != friendly.Count)
            throw new InvalidDataException("Detailed evidence lacks a unique summary for every initial friendly recipient.");
        bool IsFriendly(string? id) => id != null && friendly.ContainsKey(id);
        var firstDeath = stats.Select(s => s.FirstDeathTick).Min();
        var health = events.Where(e => IsFriendly(e.TargetId) && TowerCoverageDiagnosticMechanics.Healing(e.EventType)).ToArray();
        var regen = events.Where(e => IsFriendly(e.TargetId) && e.EventType == EventType.HealthRegeneration).ToArray();
        foreach (var s in stats)
        {
            var deaths = events.Where(e => e.TargetId == s.EntityId && e.EventType == EventType.Death).Select(e => (int?)e.Timestamp).Min();
            if (health.Where(e => e.TargetId == s.EntityId).Sum(e => (long)e.Magnitude) != s.HealingReceived
                || regen.Where(e => e.TargetId == s.EntityId).Sum(e => (long)e.Magnitude) != s.HealthRegenerated
                || events.Where(e => e.TargetId == s.EntityId).Sum(e => (long)e.FinalHealthDamage) != s.FinalHealthDamage
                || deaths != s.FirstDeathTick)
                throw new InvalidDataException("Detailed events do not reconcile to saved recipient health/death statistics: " + s.EntityId);
        }
        var logRoutes = TowerCoverageDiagnosticMechanics.LogRoutes(b.PreparedParticipants, inventory);
        var matches = events.Select((e, i) => TowerCoverageDiagnosticMechanics.Match(i, e, logRoutes))
            .Where(m => m.Status != "Unmatched" || TowerCoverageDiagnosticMechanics.Application(m.EventType)).ToArray();
        var instances = new List<object>();
        foreach (var (actor, p) in friendly.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var equipped = p.GetProperty("essences").EnumerateArray().Select(e => e.GetProperty("essenceDefinitionId").GetString()).ToHashSet();
            foreach (var route in routes.Where(r => equipped.Contains(r.EssenceId)))
            {
                var key = actor + "/" + route.EvidenceKey;
                var hits = matches.Where(m => m.Status == "Unique" && m.CandidateKeys[0] == key).ToArray();
                var applications = hits.Where(m => TowerCoverageDiagnosticMechanics.Application(m.EventType) && m.Magnitude != 0).ToArray();
                instances.Add(new { ActorId = actor, route.EssenceId, route.Category, route.EvidenceKey, route.Compatible,
                    Status = applications.Length > 0 ? "ObservedApplication" : "NoObservedApplication",
                    ApplicationEventCount = applications.Length, ApplicationEventsBeforeFirstDeath = applications.Count(e => firstDeath == null || e.Tick < firstDeath),
                    FirstApplicationTick = applications.Select(e => (int?)e.Tick).Min(),
                    Recipients = applications.Select(e => e.TargetId).Distinct().Order(StringComparer.Ordinal).ToArray(),
                    AmbiguousEventIndices = matches.Where(m => m.Status == "Ambiguous" && m.CandidateKeys.Contains(key)).Select(m => m.EventIndex).ToArray(),
                    EventIndices = hits.Select(m => m.EventIndex).ToArray() });
            }
        }
        var windows = new List<object>();
        var width = checked(windowSeconds * b.TicksPerSecond);
        for (var start = 0; start <= b.Summary.DurationTicks; start += width)
        {
            var es = events.Where(e => e.Timestamp >= start && e.Timestamp < start + width && IsFriendly(e.TargetId)).ToArray();
            windows.Add(new { StartTick = start, EndExclusiveTick = start + width, ObservedThroughTick = Math.Min(start + width - 1, b.Summary.DurationTicks),
                RestoredHealth = es.Where(e => TowerCoverageDiagnosticMechanics.Healing(e.EventType)).Sum(e => (long)e.Magnitude),
                RegeneratedHealth = es.Where(e => e.EventType == EventType.HealthRegeneration).Sum(e => (long)e.Magnitude),
                FinalHealthDamage = es.Sum(e => (long)e.FinalHealthDamage), Deaths = es.Count(e => e.EventType == EventType.Death) });
        }
        return new { Status = "VerifiedDescriptiveEvidence", b.Seed, b.TicksPerSecond, FirstDeathTick = firstDeath,
            Reconciliation = "PerRecipientHealingRegenerationDamageAndFirstDeathVerified",
            RestoredHealth = health.Sum(e => (long)e.Magnitude), RegeneratedHealth = regen.Sum(e => (long)e.Magnitude),
            RestoredHealthBeforeFirstDeath = health.Where(e => firstDeath == null || e.Timestamp < firstDeath).Sum(e => (long)e.Magnitude),
            RegeneratedHealthBeforeFirstDeath = regen.Where(e => firstDeath == null || e.Timestamp < firstDeath).Sum(e => (long)e.Magnitude),
            RestoredHealthBefore40Seconds = health.Where(e => e.Timestamp < 40 * b.TicksPerSecond).Sum(e => (long)e.Magnitude),
            RegeneratedHealthBefore40Seconds = regen.Where(e => e.Timestamp < 40 * b.TicksPerSecond).Sum(e => (long)e.Magnitude),
            FirstDeathTickDamage = events.Where(e => e.Timestamp == firstDeath && e.FinalHealthDamage > 0
                && stats.Any(s => s.EntityId == e.TargetId && s.FirstDeathTick == firstDeath))
                .Select(e => new { e.Source, e.TargetId, e.DamageType, e.FinalHealthDamage }).ToArray(),
            StaggerEvents = events.Select((e, i) => (e, i)).Where(x => x.e.EventType is EventType.StaggerApplied or EventType.StaggerBroken or EventType.StaggerRecovered)
                .Select(x => new { EventIndex = x.i, x.e.Timestamp, x.e.ActorId, x.e.TargetId, x.e.Source, x.e.StatsSource, x.e.EventType,
                    x.e.Magnitude, x.e.CombatEntity }).ToArray(),
            DenialStatistics = b.Summary.Statistics.OrderBy(s => s.EntityId, StringComparer.Ordinal)
                .Select(s => new { s.EntityId, s.Team, s.ActionDeniedTicks, s.StaggeredTicks, s.StunnedOrFrozenTicks }).ToArray(),
            HealingBySourceAndOwner = health.GroupBy(e => (e.Source, e.ActorId)).OrderBy(g => g.Key.Source, StringComparer.Ordinal).ThenBy(g => g.Key.ActorId, StringComparer.Ordinal)
                .Select(g => new { g.Key.Source, g.Key.ActorId, RestoredHealth = g.Sum(e => (long)e.Magnitude),
                    Recipients = g.Where(e => e.Magnitude > 0).Select(e => e.TargetId).Distinct().Order(StringComparer.Ordinal).ToArray() }).ToArray(),
            Windows = windows, Instances = instances, Matches = matches,
            UnmatchedApplicationCount = matches.Count(m => m.Status == "Unmatched"), AmbiguousCount = matches.Count(m => m.Status == "Ambiguous") };
    }
}
