using BalanceHarness;
namespace EssenceSystem.Tests;

public sealed class BalanceHarnessJointPartyTests
{
    static readonly Dictionary<string, string> Families = new() { ["a"]="a", ["b"]="b", ["c"]="c", ["d"]="d" };
    static BossJointPartySlot Slot(int slot, params string[][] recipes) => new(slot, recipes);
    static readonly string[][] Pool = [["a","b"], ["a","c"], ["b","d"]];

    [Fact] public void Exhaustive_small_inventory_and_party_sizes_match_all_assignments()
    {
        for (var size = 1; size <= 3; size++)
        for (var inventory = 0; inventory < 81; inventory++)
        {
            var value = inventory; var copies = new Dictionary<string,int>();
            foreach (var id in Families.Keys) { copies[id] = value % 3; value /= 3; }
            var slots = Enumerable.Range(1,size).Select(s => Slot(s,Pool)).ToArray();
            var expected = new HashSet<string>(StringComparer.Ordinal);
            for (var assignment = 0; assignment < (int)Math.Pow(3,size); assignment++)
            {
                var n = assignment; var chosen = new List<string[]>();
                for (var i = 0; i < size; i++) { chosen.Add(Pool[n%3]); n/=3; }
                var distinct = chosen.Select(r => HarnessJson.Hash(r)).Distinct().Count();
                if (distinct < Math.Min(size,2) || chosen.GroupBy(r => HarnessJson.Hash(r)).Any(g => g.Count()>2)
                    || chosen.SelectMany(r => r).GroupBy(id => id).Any(g => g.Count()>copies[g.Key])) continue;
                expected.Add(string.Join("|",chosen.Select(r => string.Join(",",r))));
            }
            var result = TowerJointPartyAllocator.Allocate(Families,slots,2,copies,Math.Min(size,2),2,maximumParties:64);
            Assert.True(result.SearchExhausted); Assert.Equal(expected.Count>0,result.Feasible);
            Assert.Equal(expected.Order().ToArray(),result.Parties.Select(p => string.Join("|",p.Placements.Select(r => string.Join(",",r.EssenceIds)))).Order().ToArray());
        }
    }

    [Fact] public void Constrained_slots_are_filled_without_spending_their_only_copy_elsewhere()
    {
        var result = TowerJointPartyAllocator.Allocate(Families,[Slot(1,["a"],["b"]),Slot(2,["a"])],1,new Dictionary<string,int>{{"a",1},{"b",1}});
        var party=Assert.Single(result.Parties);Assert.Equal(new[]{"b"},party.Placements[0].EssenceIds);Assert.Equal(new[]{"a"},party.Placements[1].EssenceIds);
        Assert.Equal(1,party.UsedCopies["a"]);Assert.Equal(1,party.UsedCopies["b"]);
    }

    [Fact] public void Shared_copy_shortage_is_not_treated_as_per_character_inventory()
    {
        var result=TowerJointPartyAllocator.Allocate(Families,[Slot(1,["a"]),Slot(2,["a"])],1,new Dictionary<string,int>{{"a",1}});
        Assert.False(result.Feasible);Assert.True(result.SearchExhausted);Assert.Empty(result.Parties);
    }

    [Fact] public void Distinct_and_repeated_loadout_constraints_are_enforced()
    {
        var slots=new[]{Slot(1,["a"],["b"]),Slot(2,["a"],["b"])};
        var result=TowerJointPartyAllocator.Allocate(Families,slots,1,minimumDistinctRecipes:2,maximumUsesPerRecipe:1);
        Assert.Equal(2,result.Parties.Count);Assert.All(result.Parties,p=>Assert.Equal(2,p.DistinctRecipes));
    }

    [Fact] public void Canonicalization_and_duplicate_recipes_preserve_deterministic_output()
    {
        var a=TowerJointPartyAllocator.Allocate(Families,[Slot(1,["a","b"],["c","d"]),Slot(2,["a","b"],["c","d"])],2);
        var b=TowerJointPartyAllocator.Allocate(Families.Reverse().ToDictionary(p=>p.Key,p=>p.Value),
            [Slot(2,["d","c"],["b","a"],["a","b"]),Slot(1,["d","c"],["b","a"])],2);
        Assert.Equal(HarnessJson.Hash(a),HarnessJson.Hash(b));Assert.Equal(4,a.Parties.Count);
    }

    [Fact] public void Search_and_candidate_check_caps_report_incomplete_results()
    {
        var slots=new[]{Slot(1,["a"],["b"]),Slot(2,["a"],["b"])};
        var states=TowerJointPartyAllocator.Allocate(Families,slots,1,maximumStates:1);
        Assert.False(states.SearchExhausted);Assert.Equal("state-limit",states.StopReason);Assert.Equal(1,states.VisitedStates);
        var checks=TowerJointPartyAllocator.Allocate(Families,slots,1,maximumCandidateChecks:1);
        Assert.False(checks.SearchExhausted);Assert.Equal("candidate-check-limit",checks.StopReason);Assert.Equal(1,checks.CandidateChecks);
    }

    [Fact] public void Party_cap_preserves_complete_parties_only()
    {
        var result=TowerJointPartyAllocator.Allocate(Families,[Slot(1,["a"],["b"])],1,maximumParties:1);
        Assert.True(result.Feasible);Assert.False(result.SearchExhausted);Assert.Equal("party-limit",result.StopReason);
        Assert.Single(result.Parties);Assert.Single(result.Parties[0].Placements);
    }

    [Fact] public void Empty_pools_missing_copies_and_family_conflicts_are_explicit()
    {
        var empty=TowerJointPartyAllocator.Allocate(Families,[Slot(1)],1);Assert.False(empty.Feasible);Assert.True(empty.SearchExhausted);
        Assert.False(TowerJointPartyAllocator.Allocate(Families,[Slot(1,["a"])],1,new Dictionary<string,int>()).Feasible);
        var families=new Dictionary<string,string>(Families){["b"]="A"};
        Assert.Throws<InvalidDataException>(()=>TowerJointPartyAllocator.Allocate(families,[Slot(1,["a","b"])],2));
    }

    [Fact] public void Invalid_slots_partial_recipes_and_bounds_are_rejected()
    {
        Assert.Throws<InvalidDataException>(()=>TowerJointPartyAllocator.Allocate(Families,[Slot(2,["a"])],1));
        Assert.Throws<InvalidDataException>(()=>TowerJointPartyAllocator.Allocate(Families,[Slot(1,["a"])],2));
        Assert.Throws<InvalidDataException>(()=>TowerJointPartyAllocator.Allocate(Families,[Slot(1,["a","a"])],2));
        Assert.Throws<InvalidDataException>(()=>TowerJointPartyAllocator.Allocate(Families,[Slot(1,["a"])],1,maximumCandidateChecks:1_000_001));
    }

    [Fact] public void Cancellation_and_input_immutability_are_preserved()
    {
        var slots=new[]{Slot(1,Pool),Slot(2,Pool)};var before=HarnessJson.Hash(slots);
        TowerJointPartyAllocator.Allocate(Families,slots,2);Assert.Equal(before,HarnessJson.Hash(slots));
        using var stop=new CancellationTokenSource();stop.Cancel();
        Assert.Throws<OperationCanceledException>(()=>TowerJointPartyAllocator.Allocate(Families,slots,2,cancellationToken:stop.Token));
    }
}
