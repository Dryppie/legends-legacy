using Application.Interfaces.Services.LL.Essences;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class AbilityBalanceAuditServiceTests
{
    [Fact]
    public void Audit_runs_directly_with_normalized_local_defaults()
    {
        var simulator = new RecordingSimulator();
        var service = new AbilityBalanceAuditService(simulator, new EmptyCatalogProvider());

        var report = service.Run(new AbilityBalanceAuditRequest(), CancellationToken.None);

        Assert.Equal(6, simulator.Requests.Count);
        var screeningRequest = simulator.Requests[0];
        Assert.Equal(10, screeningRequest.EquipmentTier);
        Assert.Equal("Epic", screeningRequest.EquipmentRarity);
        Assert.Equal("Balanced", screeningRequest.EquipmentProfile);
        Assert.Equal(3, screeningRequest.TeamSize);
        Assert.Equal(1000, screeningRequest.CandidatePoolSize);
        Assert.Equal(25000, screeningRequest.BattleCount);
        Assert.Equal([1337, 2027, 9001], simulator.Requests.Take(3).Select(request => request.RandomSeed));
        Assert.Equal([1337, 2027, 9001], simulator.Requests.Skip(3).Select(request => request.RandomSeed));
        Assert.All(simulator.Requests.Skip(3), request => Assert.Equal(10, request.BattleCount));
        Assert.All(simulator.Requests.Skip(3), request => Assert.Equal(100, request.TopResults));
        Assert.Equal(75030, report.TotalBattlesRun);
        Assert.Equal([1337, 2027, 9001], report.RandomSeeds);
        Assert.All(report.Finalists, finalist => Assert.Equal(3, finalist.SeedResults?.Count));
        var matchup = Assert.Single(report.FinalistMatchups!);
        Assert.Equal("team-0", matchup.FirstSignature);
        Assert.Equal("team-1", matchup.SecondSignature);
        Assert.Equal(30, matchup.Battles);
    }

    [Fact]
    public void Audit_honors_the_request_cancellation_token()
    {
        var service = new AbilityBalanceAuditService(
            new RecordingSimulator(),
            new EmptyCatalogProvider());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            service.Run(new AbilityBalanceAuditRequest(), cancellation.Token));
    }

    [Fact]
    public void Audit_accumulates_finalist_matchup_samples_across_all_seeds()
    {
        var service = new AbilityBalanceAuditService(
            new RecordingSimulator(),
            new EmptyCatalogProvider());

        var report = service.Run(
            new AbilityBalanceAuditRequest(
                FinalistBattleCount: 34,
                RandomSeeds: [1337, 2027, 9001]),
            CancellationToken.None);

        var matchup = Assert.Single(report.FinalistMatchups!);
        Assert.Equal(102, matchup.Battles);
    }

    [Fact]
    public void Audit_supports_full_fifteen_member_tower_rosters()
    {
        var simulator = new RecordingSimulator();
        var service = new AbilityBalanceAuditService(simulator, new EmptyCatalogProvider());

        service.Run(
            new AbilityBalanceAuditRequest(
                TeamSize: 99,
                ScreeningBattleCount: 1,
                FinalistCount: 2,
                FinalistBattleCount: 1,
                ValidationBattleCount: 1,
                RandomSeeds: [17]),
            CancellationToken.None);

        Assert.NotEmpty(simulator.Requests);
        Assert.All(simulator.Requests, request => Assert.Equal(15, request.TeamSize));
    }

    private sealed class RecordingSimulator : IAbilityBalanceSimulator
    {
        public List<AbilityBalanceSimulationRequest> Requests { get; } = [];

        public AbilityBalanceSimulationReport Run(
            AbilityBalanceSimulationRequest request,
            CancellationToken cancellationToken = default,
            Action<AbilityBalanceSimulationProgress>? progress = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request);
            var teams = request.CandidateTeams ??
            [
                new AbilityBalanceTeamLoadout([new AbilityBalanceParticipantLoadout(["essence-a"])]),
                new AbilityBalanceTeamLoadout([new AbilityBalanceParticipantLoadout(["essence-b"])])
            ];
            var combinations = teams.Select((team, index) => new AbilityBalanceCombinationResult(
                $"team-{index}",
                $"Team {index}",
                team.Participants,
                request.BattleCount,
                request.BattleCount / 2,
                request.BattleCount / 2,
                0,
                0.5,
                0.5,
                0,
                10,
                100,
                100)).ToList();
            var essenceResults = teams
                .SelectMany(team => team.Participants)
                .SelectMany(participant => participant.EssenceIds)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(id => new AbilityBalanceEssenceResult(
                    id,
                    id,
                    1,
                    request.BattleCount,
                    request.BattleCount / 2,
                    request.BattleCount / 2,
                    0,
                    0.5,
                    0,
                    0,
                    0.49,
                    0.51,
                    10,
                    100,
                    100,
                    "Healthy"))
                .ToList();

            return new AbilityBalanceSimulationReport(
                request.CandidateTeams is null ? "RandomPool" : "SavedVsSaved",
                request.BattleCount,
                request.BattleCount,
                request.TeamSize,
                request.EssencesPerParticipant,
                request.RandomSeed,
                teams.Count,
                request.CandidatePoolSize,
                2,
                request.EquipmentTier,
                request.EquipmentRarity,
                request.EquipmentProfile,
                new Dictionary<string, float>(),
                [],
                combinations,
                essenceResults,
                [],
                combinations.Count < 2
                    ? []
                    :
                    [
                        new AbilityBalanceMatchupResult(
                            combinations[0].Signature,
                            combinations[1].Signature,
                            request.BattleCount,
                            request.BattleCount / 2,
                            request.BattleCount / 2,
                            0,
                            0.5)
                    ]);
        }
    }

    private sealed class EmptyCatalogProvider : IAbilityCatalogProvider
    {
        private static readonly AbilityCatalog Catalog = new(
            [],
            [],
            [],
            new Dictionary<string, string>());

        public AbilityCatalog GetCatalog() => Catalog;
    }
}
