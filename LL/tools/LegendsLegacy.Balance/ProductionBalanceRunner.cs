using Application.Interfaces.Services.LL.Essences;
using Services.LL.Combat.Engine;
using System.Globalization;

namespace LegendsLegacy.Balance;

public sealed class ProductionBalanceRunner(
    IAbilityCatalogProvider catalogProvider,
    IEssenceDefinitionRepository essenceDefinitions,
    IAbilityBalanceSimulator simulator,
    GearPackageFactory gearPackages,
    TimeProvider timeProvider)
{
    public const int BalanceSchemaVersion = 2;
    public const string SmokeScenarioId = "production-essence-smoke-1v1";

    public BalanceRunReport Run(BalanceRunRequest request)
    {
        var catalog = catalogProvider.GetCatalog();
        var essences = essenceDefinitions.GetAll()
            .Where(essence =>
                !string.IsNullOrWhiteSpace(essence.Id)
                && !essence.Id.Equals("essence.training", StringComparison.OrdinalIgnoreCase))
            .OrderBy(essence => essence.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (essences.Length < 2)
            throw new InvalidOperationException("The production catalog must contain at least two usable Essences.");

        var friendlyEssenceId = essences[0].Id;
        var hostileEssenceId = essences[1].Id;
        var simulation = simulator.Run(new AbilityBalanceSimulationRequest(
            BattleCount: 1,
            TeamSize: 1,
            EssencesPerParticipant: 1,
            RandomSeed: request.Seed,
            TopResults: 2,
            CandidatePoolSize: 2,
            CandidateTeams:
            [
                new AbilityBalanceTeamLoadout(
                    [new AbilityBalanceParticipantLoadout([friendlyEssenceId])]),
                new AbilityBalanceTeamLoadout(
                    [new AbilityBalanceParticipantLoadout([hostileEssenceId])])
            ]));
        var battle = simulation.BattleSummaries.Single();
        var regionOneGearPackages = gearPackages.CreateRegionOneAnchors();
        var createdAtUtc = timeProvider.GetUtcNow();
        var runId = CreateRunId(createdAtUtc);
        var engineVersion = typeof(FastCombatEngine).Assembly.GetName().Version?.ToString() ?? "unknown";

        return new BalanceRunReport(
            new BalanceRunMetadata(
                runId,
                createdAtUtc,
                simulation.RandomSeed,
                BalanceSchemaVersion,
                AbilityBalanceSimulator.AlgorithmVersion,
                engineVersion,
                request.GitCommitHash),
            new BalanceContentSummary(
                catalog.Abilities.Count,
                catalog.Statuses.Count,
                catalog.Summons.Count,
                essences.Length),
            new BalanceSimulationSummary(
                SmokeScenarioId,
                battle.FriendlyDisplayName,
                friendlyEssenceId,
                battle.HostileDisplayName,
                hostileEssenceId,
                battle.Outcome,
                battle.Duration,
                battle.FriendlyDamageDone,
                battle.FriendlyDamageTaken,
                battle.HostileDamageDone,
                battle.HostileDamageTaken),
            regionOneGearPackages);
    }

    private static string CreateRunId(DateTimeOffset createdAtUtc) =>
        $"{createdAtUtc.ToUniversalTime().ToString("yyyyMMddTHHmmssfff'Z'", CultureInfo.InvariantCulture)}-{Guid.NewGuid():N}"[..28];
}

public sealed record BalanceRunRequest(int Seed, string? GitCommitHash = null);

public sealed record BalanceRunReport(
    BalanceRunMetadata Metadata,
    BalanceContentSummary Content,
    BalanceSimulationSummary Simulation,
    IReadOnlyList<GearPackageSnapshot> GearPackages);

public sealed record BalanceRunMetadata(
    string RunId,
    DateTimeOffset CreatedAtUtc,
    int Seed,
    int BalanceSchemaVersion,
    int SimulatorAlgorithmVersion,
    string CombatEngineVersion,
    string? GitCommitHash);

public sealed record BalanceContentSummary(
    int AbilityCount,
    int StatusCount,
    int SummonCount,
    int EssenceCount);

public sealed record BalanceSimulationSummary(
    string ScenarioId,
    string FriendlyBuild,
    string FriendlyEssenceId,
    string HostileBuild,
    string HostileEssenceId,
    string Outcome,
    int DurationTicks,
    int FriendlyDamageDone,
    int FriendlyDamageTaken,
    int HostileDamageDone,
    int HostileDamageTaken);
