using Domain.Models.Combat;

namespace Services.LL.Combat.Engine;

/// <summary>
/// Runs a mechanic-isolation simulation against authored Stagger definitions. It deliberately
/// excludes damage and survivability so threshold, recovery, and party-size behavior can be
/// assessed without encounter outcome noise.
/// </summary>
public sealed class StaggerCalibrationRunner
{
    public StaggerCalibrationReport Run(
        StaggerCalibrationCatalog catalog,
        StaggerCalibrationRunOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        options ??= new StaggerCalibrationRunOptions();
        if (options.SampleCount is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(options), "Sample count must be between 1 and 1,000.");

        var seeds = CreateSeeds(catalog.Seeds, options.SampleCount);
        var encounters = catalog.Encounters
            .Where(encounter => Includes(options.EncounterIds, encounter.Id))
            .ToList();
        var cohorts = catalog.Cohorts
            .Where(cohort => Includes(options.CohortIds, cohort.Id))
            .ToList();
        var profiles = catalog.Profiles
            .Where(profile => Includes(options.ProfileIds, profile.Id))
            .ToList();
        if (encounters.Count == 0 || cohorts.Count == 0 || profiles.Count == 0)
            throw new InvalidOperationException("The Stagger calibration filters did not select any result rows.");

        var results = new List<StaggerCalibrationResult>();
        foreach (var encounter in encounters)
        {
            foreach (var cohort in cohorts)
            {
                var participants = Math.Max(
                    1,
                    (int)Math.Round(
                        encounter.Definition.ReferenceParticipantCount * cohort.ParticipantMultiplier,
                        MidpointRounding.AwayFromZero));
                foreach (var profile in profiles)
                {
                    var samples = seeds.Select(seed => RunSample(
                        encounter.Definition,
                        participants,
                        profile,
                        catalog.EvaluationDurationTicks,
                        seed)).ToList();
                    results.Add(Aggregate(
                        encounter,
                        cohort,
                        participants,
                        profile,
                        catalog.EvaluationDurationTicks,
                        samples));
                }
            }
        }

        var exceptions = Assess(results, profiles);
        return new StaggerCalibrationReport(results, exceptions);
    }

    private static StaggerCalibrationSample RunSample(
        BossStaggerDefinition definition,
        int participantCount,
        StaggerCalibrationControlProfile profile,
        int durationTicks,
        int seed)
    {
        var state = new RuntimeStaggerState(definition, participantCount);
        var random = new Random(seed);
        var contributorCount = Math.Max(
            1,
            (int)Math.Round(participantCount * profile.ContributorShare, MidpointRounding.AwayFromZero));
        var nextAttempts = Enumerable.Range(0, contributorCount)
            .Select(_ => random.Next(1, profile.IntervalTicks + 1))
            .ToArray();
        var attempted = 0;
        var accepted = 0;
        var staggeredTicks = 0;
        var breakTicks = new List<int>();

        for (var tick = 1; tick <= durationTicks; tick++)
        {
            state.Tick();
            for (var contributor = 0; contributor < contributorCount; contributor++)
            {
                if (nextAttempts[contributor] != tick)
                    continue;

                attempted += profile.StaggerPower;
                if (random.Next(1, 101) <= profile.SuccessPercent)
                {
                    accepted += state.Apply(profile.StaggerPower, out var broke);
                    if (broke)
                        breakTicks.Add(tick);
                }

                nextAttempts[contributor] += profile.IntervalTicks;
            }

            if (state.IsStaggered)
                staggeredTicks++;
        }

        return new StaggerCalibrationSample(
            attempted,
            accepted,
            breakTicks,
            staggeredTicks,
            definition.MaximumBreaks.HasValue
            && breakTicks.Count >= definition.MaximumBreaks.Value);
    }

    private static StaggerCalibrationResult Aggregate(
        StaggerCalibrationEncounter encounter,
        StaggerCalibrationParticipantCohort cohort,
        int participantCount,
        StaggerCalibrationControlProfile profile,
        int evaluationDurationTicks,
        IReadOnlyList<StaggerCalibrationSample> samples)
    {
        var firstBreakTicks = samples
            .Where(sample => sample.BreakTicks.Count > 0)
            .Select(sample => sample.BreakTicks[0])
            .ToList();
        var attempted = samples.Average(sample => sample.Attempted);
        var accepted = samples.Average(sample => sample.Accepted);
        return new StaggerCalibrationResult(
            encounter.Id,
            encounter.ContentType,
            encounter.Name,
            encounter.Source,
            cohort.Id,
            cohort.IsAssessmentCohort,
            participantCount,
            encounter.Definition.ReferenceParticipantCount,
            profile.Id,
            Math.Max(1, (int)Math.Round(
                participantCount * profile.ContributorShare,
                MidpointRounding.AwayFromZero)),
            encounter.Definition.BaseThreshold,
            encounter.Definition.CalculateThreshold(participantCount, 0),
            samples.Count,
            attempted,
            accepted,
            attempted > 0 ? 100d * accepted / attempted : 0,
            samples.Average(sample => sample.BreakTicks.Count),
            samples.Min(sample => sample.BreakTicks.Count),
            samples.Max(sample => sample.BreakTicks.Count),
            firstBreakTicks.Count > 0 ? firstBreakTicks.Average() : null,
            100d * samples.Count(sample => sample.BreakTicks.Count > 0) / samples.Count,
            100d * samples.Average(sample => sample.StaggeredTicks) / evaluationDurationTicks,
            samples.Count(sample => sample.ReachedBreakCap) / (double)samples.Count);
    }

    private static IReadOnlyList<StaggerCalibrationException> Assess(
        IReadOnlyList<StaggerCalibrationResult> results,
        IReadOnlyList<StaggerCalibrationControlProfile> profiles)
    {
        var profilesById = profiles.ToDictionary(profile => profile.Id, StringComparer.OrdinalIgnoreCase);
        var exceptions = new List<StaggerCalibrationException>();
        foreach (var result in results.Where(result => result.IsAssessmentCohort))
        {
            var profile = profilesById[result.ProfileId];
            AddOutsideBand(
                exceptions,
                result,
                "AverageBreaks",
                result.AverageBreaks,
                profile.MinimumBreaks,
                profile.MaximumBreaks);
            if ((profile.MinimumFirstBreakTick.HasValue || profile.MaximumFirstBreakTick.HasValue)
                && (result.AverageFirstBreakTick.HasValue || profile.MinimumBreaks > 0))
            {
                AddOutsideBand(
                    exceptions,
                    result,
                    "AverageFirstBreakTick",
                    result.AverageFirstBreakTick
                    ?? (profile.MaximumFirstBreakTick.HasValue
                        ? profile.MaximumFirstBreakTick.Value + 1d
                        : 0d),
                    profile.MinimumFirstBreakTick ?? 0,
                    profile.MaximumFirstBreakTick ?? double.MaxValue);
            }

            AddOutsideBand(
                exceptions,
                result,
                "StaggerUptimePercent",
                result.AverageStaggerUptimePercent,
                0,
                profile.MaximumStaggerUptimePercent);
            AddOutsideBand(
                exceptions,
                result,
                "BreakCapRate",
                result.BreakCapRate,
                0,
                profile.MaximumBreakCapRate);
        }

        foreach (var group in results.GroupBy(
                     result => $"{result.EncounterId}|{result.ProfileId}",
                     StringComparer.OrdinalIgnoreCase))
        {
            var spread = group.Max(result => result.AverageBreaks)
                         - group.Min(result => result.AverageBreaks);
            if (spread <= 1d)
                continue;

            var reference = group.FirstOrDefault(result => result.IsAssessmentCohort) ?? group.First();
            exceptions.Add(new StaggerCalibrationException(
                reference.EncounterId,
                reference.CohortId,
                reference.ProfileId,
                "PartySizeBreakSpread",
                spread,
                0,
                1d));
        }

        return exceptions
            .OrderBy(exception => exception.EncounterId, StringComparer.Ordinal)
            .ThenBy(exception => exception.ProfileId, StringComparer.Ordinal)
            .ThenBy(exception => exception.Metric, StringComparer.Ordinal)
            .ToList();
    }

    private static void AddOutsideBand(
        ICollection<StaggerCalibrationException> exceptions,
        StaggerCalibrationResult result,
        string metric,
        double actual,
        double minimum,
        double maximum)
    {
        if (actual >= minimum && actual <= maximum)
            return;

        exceptions.Add(new StaggerCalibrationException(
            result.EncounterId,
            result.CohortId,
            result.ProfileId,
            metric,
            actual,
            minimum,
            maximum));
    }

    private static bool Includes(IReadOnlyCollection<string>? filter, string value) =>
        filter is null
        || filter.Count == 0
        || filter.Contains(value, StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyList<int> CreateSeeds(
        IReadOnlyList<int> configured,
        int? requestedCount)
    {
        var count = requestedCount ?? configured.Count;
        var result = configured.Take(count).ToList();
        var used = result.ToHashSet();
        for (var index = result.Count; index < count; index++)
        {
            var candidate = (int)((0x9E3779B9u * (uint)(index + 1) ^ 0x5A5A5A5Au) & 0x7FFF_FFFFu);
            candidate = Math.Max(1, candidate);
            while (!used.Add(candidate))
                candidate = candidate == int.MaxValue ? 1 : candidate + 1;
            result.Add(candidate);
        }

        return result;
    }
}

public enum StaggerCalibrationContentType
{
    Tower,
    Raid,
    RaidPlus
}

public sealed record StaggerCalibrationCatalog(
    int Version,
    int EvaluationDurationTicks,
    IReadOnlyList<int> Seeds,
    IReadOnlyList<StaggerCalibrationParticipantCohort> Cohorts,
    IReadOnlyList<StaggerCalibrationControlProfile> Profiles,
    IReadOnlyList<StaggerCalibrationEncounter> Encounters);

public sealed record StaggerCalibrationEncounter(
    string Id,
    StaggerCalibrationContentType ContentType,
    string Name,
    BossStaggerDefinition Definition,
    string Source);

public sealed record StaggerCalibrationParticipantCohort(
    string Id,
    double ParticipantMultiplier,
    bool IsAssessmentCohort);

public sealed record StaggerCalibrationControlProfile(
    string Id,
    string Description,
    double ContributorShare,
    int StaggerPower,
    int IntervalTicks,
    int SuccessPercent,
    double MinimumBreaks,
    double MaximumBreaks,
    int? MinimumFirstBreakTick,
    int? MaximumFirstBreakTick,
    double MaximumStaggerUptimePercent,
    double MaximumBreakCapRate);

public sealed record StaggerCalibrationRunOptions(
    IReadOnlyCollection<string>? EncounterIds = null,
    IReadOnlyCollection<string>? CohortIds = null,
    IReadOnlyCollection<string>? ProfileIds = null,
    int? SampleCount = null);

public sealed record StaggerCalibrationReport(
    IReadOnlyList<StaggerCalibrationResult> Results,
    IReadOnlyList<StaggerCalibrationException> Exceptions);

public sealed record StaggerCalibrationResult(
    string EncounterId,
    StaggerCalibrationContentType ContentType,
    string EncounterName,
    string Source,
    string CohortId,
    bool IsAssessmentCohort,
    int ParticipantCount,
    int ReferenceParticipantCount,
    string ProfileId,
    int ContributorCount,
    int BaseThreshold,
    int InitialThreshold,
    int SampleCount,
    double AverageAttemptedStagger,
    double AverageAcceptedStagger,
    double AverageContributionEfficiencyPercent,
    double AverageBreaks,
    int MinimumBreaks,
    int MaximumBreaks,
    double? AverageFirstBreakTick,
    double FirstBreakRatePercent,
    double AverageStaggerUptimePercent,
    double BreakCapRate);

public sealed record StaggerCalibrationException(
    string EncounterId,
    string CohortId,
    string ProfileId,
    string Metric,
    double Actual,
    double Minimum,
    double Maximum);

internal sealed record StaggerCalibrationSample(
    int Attempted,
    int Accepted,
    IReadOnlyList<int> BreakTicks,
    int StaggeredTicks,
    bool ReachedBreakCap);
