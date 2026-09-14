using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerGenerationFeedbackTests
{
    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static BossDiscoveryInputs Input()
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(5, 5);
        d = d with { Generation = new(TowerGenerationFeedback.Methods, [-101], 384, 8192, 4,
            TowerBossDiscovery.Objective, TowerGenerationFeedback.Version),
            Stages = new(4, 2, 0, 0, d.Contexts.ToDictionary(c => c.Id, _ => new BossDiscoverySchedule(
                Enumerable.Range(201, 8).ToArray(), Enumerable.Range(301, 64).ToArray(), Enumerable.Range(10001, 512).ToArray(), [],
                Enumerable.Range(501, 32).ToArray()))), MaximumBattles = 63488 };
        return TowerBossDiscovery.GenerationInputs(d);
    }

    private static BossDiscoveryMeasurement Measure(BossDiscoveryInputs d, PartyChoice party, int wins = 0) =>
        BalanceHarnessTowerBossGenerationTests.Measure(d, party, wins, Convert.ToInt32(party.Id[..2], 16) / 3d);

    private static Task<BossGenerationResult> Run(BossDiscoveryInputs d, int wins = 0) => TowerBossGeneration.RunAsync(d,
        TowerBossPartyGenerator.FromInventory(d, Inventory.Value), (p, _, _) => Task.FromResult(Measure(d, p)),
        evaluateFeedback: d.FeedbackSeeds is null ? null : (p, _, _) => Task.FromResult(Measure(d with { DiscoverySeeds = d.FeedbackSeeds }, p, wins)));

    [Fact]
    public async Task Feedback_changes_descendants_but_preserves_v13_comparator_and_first_96_candidates()
    {
        var d = Input(); var first = await Run(d); var other = await Run(d, 16);
        Assert.Equal("Complete", first.Status); Assert.Equal("Complete", other.Status);
        var old = await Run(d with { FeedbackSeeds = null, Generation = d.Generation with {
            PolicyVersion = TowerBossGeneration.LoadoutCompositionVersion, Methods = TowerBossGeneration.LoadoutCompositionMethods } });
        Assert.Equal(HarnessJson.Hash(old.Arms[1]), HarnessJson.Hash(first.Arms[0]));
        Assert.Equal(HarnessJson.Hash(first.Arms[0]), HarnessJson.Hash(other.Arms[0]));
        Assert.NotEqual(HarnessJson.Hash(first.Arms[1].Proposals), HarnessJson.Hash(other.Arms[1].Proposals));
        Assert.Equal(384, first.Arms[0].Evaluations.Count); Assert.Equal(320, first.Arms[1].Evaluations.Count);
        Assert.Equal(first.Arms[0].Evaluations.Take(96).Select(r => r.Id), first.Arms[1].Evaluations.Take(96).Select(r => r.Id));
        Assert.Null(first.Arms[0].Feedback);
        var arm = first.Arms[1]; TowerGenerationFeedback.ValidateRounds(d, arm);
        var rounds = Assert.IsAssignableFrom<IReadOnlyList<BossFeedbackRound>>(arm.Feedback);
        Assert.Equal(TowerGenerationFeedback.Checkpoints, rounds.Select(r => r.AfterCandidates));
        Assert.Equal(16, rounds.SelectMany(r => r.Measurements).Select(r => r.Id).Distinct().Count());
        Assert.All(arm.Evaluations, row => Assert.Equal(8, row.Cells.Single().Clears.Count));
        Assert.All(rounds, round => Assert.All(round.Measurements, row => Assert.Equal(32, row.Cells.Single().Clears.Count)));
        Assert.Equal(HarnessJson.Hash(first), HarnessJson.Hash(await Run(d)));
        Assert.DoesNotContain("feedback", JsonSerializer.Serialize(old.Arms[1], HarnessJson.Options));
        var bad = arm with { Feedback = [rounds[0] with { Measurements = rounds[0].Measurements.Reverse().ToArray() }, ..rounds.Skip(1)] };
        Assert.Throws<InvalidDataException>(() => TowerGenerationFeedback.ValidateRounds(d, bad));
        Assert.Throws<InvalidDataException>(() => TowerGenerationFeedback.ValidateRounds(d, arm with { Feedback = rounds.Take(3).ToArray() }));
    }

    [Fact]
    public async Task Forty_trial_training_score_keeps_raw_measurements_and_rejects_incomplete_or_foreign_feedback()
    {
        var d = Input(); var generator = new TowerBossPartyGenerator(d, TowerBossPartyGenerator.FromInventory(d, Inventory.Value));
        var p = generator.Fresh(new Random(17), true).Party!;
        var old = Measure(d, p, 2); var fresh = Measure(d with { DiscoverySeeds = d.FeedbackSeeds! }, p, 4);
        var combined = TowerGenerationFeedback.Combine(d, old, fresh);
        Assert.Equal(.15, combined.Fitness.WorstContextWinRate, 12);
        Assert.Equal(40, combined.Cells.Single().Clears.Count);
        Assert.Equal(8, old.Cells.Single().Clears.Count); Assert.Equal(32, fresh.Cells.Single().Clears.Count);
        Assert.Throws<InvalidDataException>(() => TowerGenerationFeedback.Combine(d, old, fresh with { Id = "foreign" }));
        Assert.Throws<InvalidDataException>(() => TowerGenerationFeedback.Combine(d, old, fresh with { Cells = old.Cells }));
        Assert.Throws<InvalidDataException>(() => TowerGenerationFeedback.ValidateInputs(d with { FeedbackSeeds = d.DiscoverySeeds }));
        Assert.Throws<InvalidDataException>(() => TowerGenerationFeedback.ValidateInputs(d with { Generation = d.Generation with {
            PolicyVersion = TowerBossGeneration.LoadoutCompositionVersion, Methods = TowerBossGeneration.LoadoutCompositionMethods } }));
        var calls = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossGeneration.RunAsync(d,
            TowerBossPartyGenerator.FromInventory(d, Inventory.Value), (party, _, _) => { calls++; return Task.FromResult(Measure(d, party)); }));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task An_interrupted_feedback_round_cannot_be_reported_as_complete_or_spend_later_allocations()
    {
        var d = Input(); var feedback = 0; var original = 0;
        var result = await TowerBossGeneration.RunAsync(d, TowerBossPartyGenerator.FromInventory(d, Inventory.Value),
            (p, _, _) => { original++; return Task.FromResult(Measure(d, p)); },
            evaluateFeedback: (p, _, _) => {
                if (++feedback == 2) throw new InvalidDataException("Injected incomplete feedback round");
                return Task.FromResult(Measure(d with { DiscoverySeeds = d.FeedbackSeeds! }, p));
            });
        Assert.NotEqual("Complete", result.Status); Assert.Equal(2, feedback); Assert.Equal(480, original);
    }
}
