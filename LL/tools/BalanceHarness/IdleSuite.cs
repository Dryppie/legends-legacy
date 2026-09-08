using System.Globalization;
using System.Text.RegularExpressions;
using Common.Randomness;
using Services.LL.Combat.Engine;
using Services.LL.PowerRatings;

namespace BalanceHarness;

public sealed record IdleSuiteDefinition(int SchemaVersion, string Id, string Description,
    int SamplesPerCell, DateTimeOffset StartsAt, IReadOnlyList<IdleProgressionStage> Stages);
public sealed record IdleProgressionStage(string Id, string Name, string AreaId,
    IReadOnlyList<string> Assumptions, IReadOnlyList<EquipmentReferenceBuildDefinition> Builds,
    IReadOnlyList<IdleBenchmarkEncounter> Encounters);
public sealed record IdleBenchmarkEncounter(string Id, string Label, Guid CreatureId);
public sealed record SuiteTrial(string BattleId, int Index, int Seed);
public sealed record SuiteCell(string Id, string Stage, string Build, string Encounter,
    IdleBattleInput Input, IReadOnlyList<SuiteTrial> Trials);
public sealed record SuiteRunInput(int SchemaVersion, string SeedScheduleVersion, int MasterSeed,
    IdleSuiteDefinition Definition, IReadOnlyList<SuiteCell> Cells);

public static class IdleSuite
{
    public const string SeedScheduleVersion = "idle-suite-seeds-v1";

    public static SuiteRunInput Resolve(IdleSuiteDefinition suite, OfflineContent content,
        ThreatAndTankingOptions threat, double cadence, int masterSeed)
    {
        if (suite.SchemaVersion != 1 || suite.SamplesPerCell is < 1 or > 10000 || suite.Stages.Count == 0)
            throw new InvalidDataException("Suite requires version 1, stages and 1–10,000 samples per cell.");
        ValidateId(suite.Id);
        UniqueIds(suite.Stages.Select(x => x.Id));
        var cellCount = suite.Stages.Sum(stage => (long)stage.Builds.Count * stage.Encounters.Count);
        if (cellCount > 1000 || cellCount * suite.SamplesPerCell > 100000)
            throw new InvalidDataException("Suite exceeds the local budget of 1,000 cells or 100,000 battles.");
        var cells = new List<SuiteCell>();
        foreach (var stage in suite.Stages)
        {
            if (stage.Builds.Count == 0 || stage.Encounters.Count == 0 || stage.Assumptions.Count == 0)
                throw new InvalidDataException($"Stage '{stage.Id}' needs builds, encounters and acquisition assumptions.");
            UniqueIds(stage.Builds.Select(x => x.Id));
            UniqueIds(stage.Encounters.Select(x => x.Id));
            foreach (var build in stage.Builds)
            foreach (var encounter in stage.Encounters)
            {
                var id = $"{stage.Id}.{build.Id}.{encounter.Id}";
                var trials = Enumerable.Range(0, suite.SamplesPerCell).Select(index => new SuiteTrial(
                    $"{id}.{index + 1:D4}", index,
                    // Equal encounter/seed schedules pair alternative builds. Content hashes and
                    // enumeration order deliberately do not enter the seed identity.
                    StableRandom.Seed(SeedScheduleVersion, masterSeed.ToString(CultureInfo.InvariantCulture),
                        stage.Id, encounter.Id, index.ToString(CultureInfo.InvariantCulture)))).ToArray();
                if (trials.Select(x => x.Seed).Distinct().Count() != trials.Length)
                    throw new InvalidDataException($"Seed collision in '{id}'; choose another master seed.");
                var scenario = new IdleScenario(1, id, build.Id, stage.AreaId, encounter.CreatureId,
                    suite.StartsAt, stage.Assumptions, build);
                cells.Add(new(id, stage.Name, build.Id, encounter.Label,
                    content.CreateInput(scenario, trials[0].Seed, threat, cadence), trials));
            }
        }
        return new(1, SeedScheduleVersion, masterSeed, suite, cells);
    }

    public static IdleBattleInput BattleInput(SuiteCell cell, SuiteTrial trial) =>
        cell.Input with { Rules = cell.Input.Rules with { RandomSeed = trial.Seed } };

    private static void UniqueIds(IEnumerable<string> ids)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            ValidateId(id);
            if (!seen.Add(id)) throw new InvalidDataException($"Duplicate suite identifier '{id}'.");
        }
    }

    private static void ValidateId(string id)
    {
        if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, "^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant))
            throw new InvalidDataException("Suite identifiers require lowercase letters, digits and hyphens (maximum 64 characters).");
    }
}
