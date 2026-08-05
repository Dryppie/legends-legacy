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

        Assert.Equal(4, simulator.Requests.Count);
        var screeningRequest = simulator.Requests[0];
        Assert.Equal(10, screeningRequest.EquipmentTier);
        Assert.Equal("Epic", screeningRequest.EquipmentRarity);
        Assert.Equal("Balanced", screeningRequest.EquipmentProfile);
        Assert.Equal(1000, screeningRequest.CandidatePoolSize);
        Assert.Equal(250000, screeningRequest.BattleCount);
        Assert.Equal([1337, 2027, 9001], simulator.Requests.Take(3).Select(request => request.RandomSeed));
        Assert.Equal(500, simulator.Requests[^1].BattleCount);
        Assert.Equal(100, simulator.Requests[^1].TopResults);
        Assert.Equal(750500, report.TotalBattlesRun);
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
                []);
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
