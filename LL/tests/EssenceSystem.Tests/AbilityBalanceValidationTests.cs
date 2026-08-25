using Application.Interfaces.Services.LL.Essences;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class AbilityBalanceValidationTests
{
    [Fact]
    public void Audit_aggregates_three_contexts_and_three_healthy_replacements()
    {
        var simulator = new ValidationSimulator();
        var service = new AbilityBalanceAuditService(simulator, new EmptyCatalogProvider());

        var report = service.Run(
            new AbilityBalanceAuditRequest(
                TeamSize: 1,
                EssencesPerParticipant: 2,
                CandidatePoolSize: 10,
                ScreeningBattleCount: 5_000,
                FinalistCount: 4,
                FinalistBattleCount: 2,
                ValidationBattleCount: 10),
            CancellationToken.None);

        var validation = Assert.Single(report.ValidationResults);
        Assert.Equal("essence-flagged", validation.EssenceId);
        Assert.Equal(3, validation.ContextCount);
        Assert.Equal(3, validation.ReplacementCount);
        Assert.Equal(90, validation.Battles);
        Assert.Equal(90, report.ValidationBattlesRun);
        Assert.Equal(0.6d, validation.OriginalScore, precision: 3);
        Assert.Equal(0.4d, validation.ReplacementScore, precision: 3);
        Assert.Equal(0.2d, validation.ScoreDelta, precision: 3);
        Assert.Equal(9, simulator.ValidationRequests.Count);
        Assert.Equal(3, simulator.ValidationRequests.Select(request =>
            request.CandidateTeams![0].Participants[0].EssenceIds.Single(id =>
                id.StartsWith("context-", StringComparison.Ordinal))).Distinct().Count());
        Assert.Equal(
            ["essence-healthy-a", "essence-healthy-b", "essence-healthy-c"],
            validation.ReplacementEssenceId.Split(" | ", StringSplitOptions.None).Order());
    }

    private sealed class ValidationSimulator : IAbilityBalanceSimulator
    {
        public List<AbilityBalanceSimulationRequest> ValidationRequests { get; } = [];

        public AbilityBalanceSimulationReport Run(
            AbilityBalanceSimulationRequest request,
            CancellationToken cancellationToken = default,
            Action<AbilityBalanceSimulationProgress>? progress = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request.CandidateTeams is null)
                return ScreeningReport(request);
            if (request.CandidateTeams.Count == 2
                && request.CandidateTeams.Any(ContainsFlaggedEssence))
            {
                ValidationRequests.Add(request);
                return ValidationReport(request);
            }

            return FinalistReport(request);
        }

        private static AbilityBalanceSimulationReport ScreeningReport(
            AbilityBalanceSimulationRequest request)
        {
            var combinations = new[]
            {
                Combination("context-low", ["essence-flagged", "context-low"], 35),
                Combination("context-mid", ["essence-flagged", "context-mid"], 50),
                Combination("context-high", ["essence-flagged", "context-high"], 65),
                Combination("healthy-team", ["essence-healthy-a", "essence-healthy-b"], 50)
            };
            var essenceResults = new[]
            {
                EssenceResult("essence-flagged", 0.6d, 0.1d, "Overperforming"),
                EssenceResult("essence-healthy-a", 0.5d, 0.00d, "Healthy"),
                EssenceResult("essence-healthy-b", 0.5d, 0.01d, "Healthy"),
                EssenceResult("essence-healthy-c", 0.5d, -0.01d, "Healthy")
            };
            return Report(request, combinations, essenceResults, "RandomPool");
        }

        private static AbilityBalanceSimulationReport ValidationReport(
            AbilityBalanceSimulationRequest request)
        {
            var candidateTeams = request.CandidateTeams!;
            var original = candidateTeams.Single(ContainsFlaggedEssence);
            var replacement = candidateTeams.Single(team => !ContainsFlaggedEssence(team));
            return Report(
                request,
                [
                    Combination("original", original.Participants, 60, request.BattleCount),
                    Combination("replacement", replacement.Participants, 40, request.BattleCount)
                ],
                [],
                "SavedRoundRobin");
        }

        private static AbilityBalanceSimulationReport FinalistReport(
            AbilityBalanceSimulationRequest request) =>
            Report(
                request,
                request.CandidateTeams!
                    .Select((team, index) => Combination(
                        $"finalist-{index}",
                        team.Participants,
                        50,
                        request.BattleCount))
                    .ToArray(),
                [],
                "SavedRoundRobin");

        private static AbilityBalanceSimulationReport Report(
            AbilityBalanceSimulationRequest request,
            IReadOnlyList<AbilityBalanceCombinationResult> combinations,
            IReadOnlyList<AbilityBalanceEssenceResult> essenceResults,
            string mode) =>
            new(
                mode,
                request.BattleCount,
                request.BattleCount,
                request.TeamSize,
                request.EssencesPerParticipant,
                request.RandomSeed,
                combinations.Count,
                request.CandidatePoolSize,
                7,
                request.EquipmentTier,
                request.EquipmentRarity,
                request.EquipmentProfile,
                new Dictionary<string, float>(),
                [],
                combinations,
                essenceResults,
                []);

        private static AbilityBalanceCombinationResult Combination(
            string signature,
            IReadOnlyList<string> essenceIds,
            int scorePercent,
            int battles = 100) =>
            Combination(
                signature,
                [new AbilityBalanceParticipantLoadout(essenceIds)],
                scorePercent,
                battles);

        private static AbilityBalanceCombinationResult Combination(
            string signature,
            IReadOnlyList<AbilityBalanceParticipantLoadout> participants,
            int scorePercent,
            int battles) =>
            new(
                signature,
                signature,
                participants,
                battles,
                battles * scorePercent / 100,
                battles * (100 - scorePercent) / 100,
                0,
                scorePercent / 100d,
                (100 - scorePercent) / 100d,
                0,
                100,
                100,
                100);

        private static AbilityBalanceEssenceResult EssenceResult(
            string id,
            double score,
            double adjustedDelta,
            string classification) =>
            new(
                id,
                id,
                100,
                5_000,
                (int)(score * 5_000),
                (int)((1d - score) * 5_000),
                0,
                score,
                score - 0.5d,
                adjustedDelta,
                score - 0.01d,
                score + 0.01d,
                100,
                100,
                100,
                classification);

        private static bool ContainsFlaggedEssence(AbilityBalanceTeamLoadout team) =>
            team.Participants.SelectMany(participant => participant.EssenceIds)
                .Contains("essence-flagged", StringComparer.OrdinalIgnoreCase);
    }

    private sealed class EmptyCatalogProvider : IAbilityCatalogProvider
    {
        public AbilityCatalog GetCatalog() => new(
            [],
            [],
            [],
            new Dictionary<string, string>());
    }
}
