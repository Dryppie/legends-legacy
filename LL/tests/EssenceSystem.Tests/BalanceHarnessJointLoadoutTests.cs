using BalanceHarness;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessJointLoadoutTests
{
    static BossCoverageFeature F(string id, string kind) => new(id, kind, ["fixture:" + id + ":" + kind]);
    static readonly Dictionary<string, string> Families = new() { ["a"] = "anchor", ["b"] = "front", ["c"] = "back", ["d"] = "front" };
    static readonly BossCoverageFeature[] Features = [F("a", "enemy-pressure"), F("b", "attack-enabler"),
        F("c", "recovery"), F("d", "attack-enabler"), F("d", "protection")];
    static readonly string[] Required = ["attack-enabler", "protection", "recovery"];

    [Fact] public void Joint_completion_keeps_multi_role_provider_that_a_greedy_choice_can_block()
    {
        var result = TowerJointLoadoutConstructor.Construct(Families, Features, ["a"], Required, 3);
        Assert.True(result.Feasible); Assert.True(result.SearchExhausted);
        Assert.Equal(new[] { "a", "c", "d" }, Assert.Single(result.Recipes).EssenceIds);
        // Reserving single-role b leaves no compatible provider for protection in the remaining slot.
        var blocked = TowerJointLoadoutConstructor.Construct(Families, Features, ["a", "b"], Required, 3);
        Assert.False(blocked.Feasible); Assert.True(blocked.SearchExhausted);
    }

    [Fact] public void Exhaustive_small_catalogue_matches_feasibility_for_every_role_and_copy_subset()
    {
        var ids = Families.Keys.Order(StringComparer.Ordinal).ToArray();
        for (var roles = 1; roles < 8; roles++)
        for (var copies = 0; copies < 16; copies++)
        for (var slots = 1; slots <= 4; slots++)
        {
            var required = Required.Where((_, i) => (roles & (1 << i)) != 0).ToArray();
            var owned = ids.Select((id, i) => (id, count: (copies & (1 << i)) == 0 ? 0 : 1)).ToDictionary(p => p.id, p => p.count);
            var feasible = Enumerable.Range(0, 16).Any(bits => {
                var chosen = ids.Where((_, i) => (bits & (1 << i)) != 0).ToArray();
                return chosen.Contains("a") && chosen.Length <= slots && chosen.All(id => owned[id] > 0)
                    && chosen.Select(id => Families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() == chosen.Length
                    && required.All(kind => Features.Any(f => f.Kind == kind && chosen.Contains(f.EssenceId)));
            });
            var result = TowerJointLoadoutConstructor.Construct(Families, Features, ["a"], required, slots, owned);
            Assert.True(result.SearchExhausted); Assert.Equal(feasible, result.Feasible);
        }
    }

    [Fact] public void Input_order_does_not_change_recipes_witnesses_or_counts()
    {
        var first = TowerJointLoadoutConstructor.Construct(Families, Features, ["a"], Required, 3);
        var reversed = TowerJointLoadoutConstructor.Construct(Families.Reverse().ToDictionary(p => p.Key, p => p.Value), Features.Reverse().ToArray(), ["a"], Required.Reverse().ToArray(), 3);
        Assert.Equal(HarnessJson.Hash(first), HarnessJson.Hash(reversed));
    }

    [Fact] public void Family_case_and_available_copy_limits_are_respected()
    {
        var families = new Dictionary<string, string>(Families) { ["d"] = "ANCHOR" };
        Assert.False(TowerJointLoadoutConstructor.Construct(families, Features, ["a"], Required, 4).Feasible);
        Assert.False(TowerJointLoadoutConstructor.Construct(Families, Features, ["a"], Required, 3, new Dictionary<string, int> { ["a"] = 1, ["c"] = 1 }).Feasible);
    }

    [Fact] public void State_limit_is_not_reported_as_infeasibility_proof()
    {
        var result = TowerJointLoadoutConstructor.Construct(Families, Features, ["a"], Required, 3, maximumStates: 1);
        Assert.False(result.Feasible); Assert.False(result.SearchExhausted); Assert.Equal("state-limit", result.StopReason); Assert.Equal(1, result.VisitedStates);
    }

    [Fact] public void Recipe_limit_retains_valid_evidence_and_reports_incomplete_search()
    {
        var result = TowerJointLoadoutConstructor.Construct(new Dictionary<string, string> { ["a"] = "a", ["b"] = "b" },
            [F("a", "recovery"), F("b", "recovery")], [], ["recovery"], 1, maximumRecipes: 1);
        Assert.True(result.Feasible); Assert.False(result.SearchExhausted); Assert.Equal("recipe-limit", result.StopReason); Assert.Single(result.Recipes);
    }

    [Fact] public void Already_complete_anchor_is_preserved_without_filler()
    {
        var result = TowerJointLoadoutConstructor.Construct(Families, Features, ["d"], ["attack-enabler", "protection"], 5);
        Assert.Equal(new[] { "d" }, Assert.Single(result.Recipes).EssenceIds); Assert.True(result.SearchExhausted);
    }

    [Fact] public void Missing_role_and_incompatible_anchor_are_explicit()
    {
        var result = TowerJointLoadoutConstructor.Construct(Families, Features, ["a"], ["recurring-control"], 5);
        Assert.False(result.Feasible); Assert.True(result.SearchExhausted);
        Assert.Equal("incompatible-anchor", TowerJointLoadoutConstructor.Construct(Families, Features, ["b", "d"], Required, 5).StopReason);
    }

    [Fact] public void Cancellation_is_observed_before_any_search()
    {
        using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.Throws<OperationCanceledException>(() => TowerJointLoadoutConstructor.Construct(Families, Features, ["a"], Required, 3, cancellationToken: stop.Token));
    }

    [Fact] public void Invalid_metadata_and_unbounded_requests_are_rejected()
    {
        Assert.Throws<InvalidDataException>(() => TowerJointLoadoutConstructor.Construct(Families, Features, ["a"], Required, 6));
        Assert.Throws<InvalidDataException>(() => TowerJointLoadoutConstructor.Construct(Families, Features, ["a"], Required, 3, maximumStates: 4097));
        Assert.Throws<InvalidDataException>(() => TowerJointLoadoutConstructor.Construct(Families, Features.Append(F("a", "enemy-pressure")).ToArray(), ["a"], Required, 3));
        Assert.Throws<InvalidDataException>(() => TowerJointLoadoutConstructor.Construct(Families, [new("a", "recovery", [])], ["a"], ["recovery"], 3));
        Assert.Throws<InvalidDataException>(() => TowerJointLoadoutConstructor.Construct(Families, Features, ["unknown"], Required, 3));
    }

    [Fact] public void Witnesses_and_inputs_are_preserved()
    {
        var before = HarnessJson.Hash(new { Families, Features, Required });
        var result = TowerJointLoadoutConstructor.Construct(Families, Features, ["a"], Required, 3);
        foreach (var witness in Assert.Single(result.Recipes).Coverage)
            Assert.Contains(Features, f => f.Kind == witness.Kind && f.EssenceId == witness.EssenceId && f.EvidenceKeys.SequenceEqual(witness.EvidenceKeys));
        Assert.Equal(before, HarnessJson.Hash(new { Families, Features, Required }));
    }
}
