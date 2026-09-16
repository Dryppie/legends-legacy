using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerRefinementComparisonSeeds(int[] Historical, int[] Generation, int[] Discovery, int[] Selection, int[] Confirmation);
public sealed record TowerRefinementComparisonMember(string Id, TowerScenario Scenario, string[] Origins);
public sealed record TowerRefinementSelectionTrial(int Seed, double GuardianHealthRemainingPercent);
public sealed record TowerRefinementSelectionHealth(string CellId, string EvidenceHash, IReadOnlyList<TowerRefinementSelectionTrial> Trials);
public sealed record TowerRefinementComparisonQuality(IReadOnlyDictionary<string, RateEstimate> Rates,
    IReadOnlyDictionary<string, TowerSearchBenchmarkPair> Differences, string Primary, string Scope);

public static class TowerRefinementComparisonModel
{
    public const string Version = "tower-discovery-refinement-comparison-v1";
    public const string NovelVersion = "tower-discovery-refinement-comparison-v2";
    public const string LocalVersion = "tower-discovery-refinement-comparison-v3";
    public const string FreshFirstVersion = "tower-discovery-refinement-comparison-v4";
    public const string ZeroWinVersion = "tower-discovery-refinement-comparison-v5";
    public static string ResolveVersion(string? version) => version switch {
        null or Version => Version,
        NovelVersion => NovelVersion,
        LocalVersion => LocalVersion,
        FreshFirstVersion => FreshFirstVersion,
        ZeroWinVersion => ZeroWinVersion,
        _ => throw new InvalidDataException("Unknown refinement comparison version.")
    };
    public static string RefinementPolicy(string? version) => ResolveVersion(version) switch {
        FreshFirstVersion or ZeroWinVersion => TowerDiscoveryRefinementSearch.FreshFirstVersion,
        LocalVersion => TowerDiscoveryRefinementSearch.LocalVersion,
        NovelVersion => TowerDiscoveryRefinementSearch.NovelVersion,
        _ => TowerDiscoveryRefinementSearch.Version
    };
    public static readonly string[] Policies = ["baseline", "discovery-refinement"];
    public static readonly string[] Controls = ["team-040e60d3dbc5c127321653c47ed3a9d3", "team-49f6979895354870c89362d4abf214bb"];
    public static readonly (string Name, int Count)[] Stages = [("generation",1),("discovery",4),("selection",8),("confirmation",32)];
    public static void Require(bool condition,string message) { if(!condition)throw new InvalidDataException(message); }
    public static TowerScenario Canonical(TowerScenario s)=>s with { Id=Version,Seeds=[],
        Assumptions=["Exploratory paired policy comparison; fixed ordinal order; no acceptance claim."],
        Party=s.Party.Select(p=>p with { Build=p.Build with { EssenceIds=p.Build.EssenceIds.Order(StringComparer.Ordinal).ToArray() } }).ToArray() };
    public static string Context(TowerScenario s)=>HarnessJson.Hash(Canonical(s) with {
        Party=s.Party.Select(p=>p with { Build=p.Build with { EssenceIds=[] } }).ToArray() });
    public static TowerBossDiscoveryDefinition Definition(TowerBossDiscoveryDefinition source,TowerRefinementComparisonSeeds s,string policy,string? comparisonVersion=null)
    {
        var version = ResolveVersion(comparisonVersion);
        Require(Policies.Contains(policy),"Unknown comparison policy.");
        var values=s.Generation.Concat(s.Discovery).Concat(s.Selection).Concat(s.Confirmation).ToArray();
        Require(s.Generation.Length==1 && s.Discovery.Length==4 && s.Selection.Length==8 && s.Confirmation.Length==32
            && values.Distinct().Count()==45 && !values.Intersect(s.Historical).Any()
            && s.Historical.SequenceEqual(s.Historical.Distinct().Order()),"Invalid separated comparison schedules.");
        Require(source.References.Select(r=>r.Id).Order().SequenceEqual(Controls.Order()) && source.Contexts.Count==1,"Changed controls/context.");
        var joined=policy=="discovery-refinement";
        var d=source with { Id=version,Mode=TowerBossDiscovery.Independent,Starts=[],MaximumBattles=176,
            Generation=new([joined?TowerDiscoveryRefinementSearch.Method:TowerTeamCoverageSearch.Method],s.Generation,16,16,4,
                TowerBossDiscovery.Objective,joined?RefinementPolicy(version):TowerTeamCoverageSearch.Version),
            Stages=new(2,1,0,0,new Dictionary<string,BossDiscoverySchedule>{[source.Contexts.Single().Id]=new(s.Discovery,s.Selection,s.Confirmation,[])}),
            ExcludedCombatSeeds=s.Historical,ExecutionHash=HarnessJson.Hash(ExecutionIdentity.Current()),
            References=source.References.Select(r=>r with { Scenario=Canonical(r.Scenario) }).ToArray() };
        Require(TowerBossDiscovery.Validate(d)==new BossDiscoveryCost(64,16,32,64,0,0,176),"Changed native policy cost.");
        foreach(var r in d.References) {
            var p=TowerPartySelection.Choice("context-check",r.Scenario.Party.ToDictionary(p=>p.PartySlot,p=>p.Build.EssenceIds));
            Require(Context(r.Scenario)==Context(TowerBossDiscovery.Scenario(d,r.Context,p,[])),"Control/generated context differs.");
        }
        return d;
    }
    public static void Shared(TowerBossDiscoveryDefinition baseline,TowerBossDiscoveryDefinition joined)
    {
        Require(baseline.Id == ResolveVersion(baseline.Id) && joined.Id == baseline.Id
            && baseline.Generation.PolicyVersion==TowerTeamCoverageSearch.Version
            && joined.Generation.PolicyVersion==RefinementPolicy(baseline.Id),"Wrong comparator policies/version.");
        Require(baseline.Generation.MaximumAttemptsPerArm==16 && joined.Generation.MaximumAttemptsPerArm==16,"Wrong proposal caps.");
        Require(HarnessJson.Hash(baseline)==HarnessJson.Hash(joined with { Generation=joined.Generation with {
            PolicyVersion=baseline.Generation.PolicyVersion,Methods=baseline.Generation.Methods,MaximumAttemptsPerArm=baseline.Generation.MaximumAttemptsPerArm } }),"Comparison inputs differ beyond policy/method.");
    }
    public static TowerRefinementComparisonMember[] Nominations(TowerBossDiscoveryDefinition[] definitions,
        IReadOnlyList<TowerDiscoveryComparisonNomination> nominees)
    {
        Require(definitions.Length == 2 && definitions[0].Id == ResolveVersion(definitions[0].Id)
            && definitions[1].Id == definitions[0].Id && definitions[0].Generation.PolicyVersion == TowerTeamCoverageSearch.Version
            && definitions[1].Generation.PolicyVersion == RefinementPolicy(definitions[0].Id), "Wrong nomination definitions.");
        Require(nominees.Count == 4, "Expected the complete gated pair.");
        return nominees.Select(n => {
            var i = n.Policy == TowerTeamCoverageSearch.Version ? 0 : n.Policy == definitions[1].Generation.PolicyVersion ? 1 : -1;
            Require(i >= 0, "Wrong nomination policy."); var d = definitions[i];
            return new TowerRefinementComparisonMember(n.Party.Id, Canonical(TowerBossDiscovery.Scenario(d,
                d.Contexts.Single().Id, n.Party, [])), [Policies[i] + "-rank-" + n.Rank]);
        }).ToArray();
    }
    public static void ValidatePair(TowerBossDiscoveryDefinition[] pair)
    {
        Require(pair.Length == 2, "Expected two definitions."); Shared(pair[0], pair[1]);
        for (var i = 0; i < 2; i++) {
            var d = pair[i]; var s = d.Stages.Schedules.Values.Single();
            var seeds = new TowerRefinementComparisonSeeds(d.ExcludedCombatSeeds.ToArray(), d.Generation.Seeds.ToArray(),
                s.Discovery.ToArray(), s.Selection.ToArray(), s.Confirmation.ToArray());
            Require(HarnessJson.Hash(d) == HarnessJson.Hash(Definition(d, seeds, Policies[i], d.Id)), "Definition differs from the fixed comparison contract.");
        }
    }
    public static TowerRefinementComparisonMember[] Merge(IEnumerable<TowerRefinementComparisonMember> source)
    {
        var rows=source.ToArray();Require(rows.Length>0 && rows.All(r=>HarnessJson.Hash(r.Scenario)==HarnessJson.Hash(Canonical(r.Scenario)))
            && rows.Select(r=>Context(r.Scenario)).Distinct().Count()==1,"Cannot merge different/noncanonical contexts.");
        Require(rows.SelectMany(r=>r.Origins).Distinct().Count()==rows.Sum(r=>r.Origins.Length),"Repeated origin.");
        return rows.GroupBy(r=>TowerBossDiscovery.RecipeHash(r.Scenario.Party),StringComparer.Ordinal)
            .Select(g=>new TowerRefinementComparisonMember("team-"+g.Key[..32],g.First().Scenario,g.SelectMany(r=>r.Origins).ToArray())).ToArray();
    }
    public static TowerRefinementComparisonMember[] Screen(TowerRefinementComparisonMember[] nominations)
    {
        Require(nominations.Length==4 && nominations.SelectMany(r=>r.Origins).SequenceEqual(
            new[]{"baseline-rank-1","baseline-rank-2","discovery-refinement-rank-1","discovery-refinement-rank-2"}),"Freeze both two-team nominations before screening.");
        foreach(var p in Policies)Require(nominations.Where(n=>n.Origins[0].StartsWith(p+"-",StringComparison.Ordinal))
            .Select(n=>TowerBossDiscovery.RecipeHash(n.Scenario.Party)).Distinct().Count()==2,"Distinct nominations required within each policy.");
        return Merge(nominations);
    }
    public static TowerRefinementComparisonMember[] Finalists(TowerBalanceDefinition screen,TowerRefinementComparisonMember[] nominations,IReadOnlyList<TowerBalanceEvidence> evidence,
        string? comparisonVersion=null, IReadOnlyList<TowerRefinementSelectionHealth>? health=null)
    {
        var zeroWin = ResolveVersion(comparisonVersion) == ZeroWinVersion;
        if (zeroWin) {
            // Origins carry the frozen per-arm discovery ranks; caller enumeration is irrelevant.
            nominations = nominations.OrderBy(n => Array.IndexOf(new[] { "baseline-rank-1", "baseline-rank-2",
                "discovery-refinement-rank-1", "discovery-refinement-rank-2" }, n.Origins.Single())).ToArray();
            Require(screen.Id == "comparison-screen" && screen.Cells.All(c => c.MinimumSamples == 8 && c.Scenario.Seeds.Count == 8),
                "Zero-win selection requires the frozen selection stage, never confirmation.");
        }
        var family=Screen(nominations);TowerFeedbackBenchmark.RequireEvidence(screen,evidence);
        Require(screen.Cells.Select(c=>c.Id).SequenceEqual(family.Select(m=>m.Id)),"Screen differs from frozen nominations.");
        var rows=evidence.ToDictionary(e=>e.CellId);var result=new List<TowerRefinementComparisonMember>();
        var means = new Dictionary<string, double>(StringComparer.Ordinal);
        if (zeroWin) {
            Require(health is not null && health.Count == rows.Count && health.All(h => h is not null && h.CellId is not null)
                && health.Select(h => h.CellId).Distinct().Count() == rows.Count, "Complete selection health evidence required.");
            foreach (var h in health!) {
                Require(rows.TryGetValue(h.CellId, out var row) && h.EvidenceHash == HarnessJson.Hash(row)
                    && h.Trials is not null && h.Trials.All(t => t is not null && double.IsFinite(t.GuardianHealthRemainingPercent)
                        && t.GuardianHealthRemainingPercent is >= 0 and <= 100)
                    && h.Trials.Select(t => t.Seed).SequenceEqual(row!.Trials.Select(t => t.Seed)),
                    "Selection health differs from the complete verified evidence.");
                means.Add(h.CellId, h.Trials!.Average(t => t.GuardianHealthRemainingPercent));
            }
        }
        foreach(var policy in Policies) {
            var candidates=Enumerable.Range(1,2).Select(rank=>new { Rank=rank,Member=family.Single(m=>m.Origins.Contains(policy+"-rank-"+rank)) }).ToArray();
            var ranked=(zeroWin ? TowerZeroWinSelection.Rank(candidates,
                x=>rows[x.Member.Id].Trials.Count(t=>t.Outcome==BattleOutcome.Victory), x=>means[x.Member.Id], x=>x.Rank, x=>x.Member.Id)
                : candidates.OrderByDescending(x=>rows[x.Member.Id].Trials.Count(t=>t.Outcome==BattleOutcome.Victory)).ThenBy(x=>x.Rank)
                    .ThenBy(x=>x.Member.Id,StringComparer.Ordinal)).First();
            result.Add(ranked.Member with { Origins=[policy+"-finalist"] });
        }
        return result.ToArray();
    }
    public static TowerRefinementComparisonMember[] Family(TowerBossDiscoveryDefinition d,TowerRefinementComparisonMember[] finalists)
    {
        Require(finalists.Length==2 && finalists.SelectMany(m=>m.Origins).SequenceEqual(new[]{"baseline-finalist","discovery-refinement-finalist"}),"Two frozen policy winners required.");
        return Merge(finalists.Concat(d.References.OrderBy(r=>r.Id,StringComparer.Ordinal).Select(r=>new TowerRefinementComparisonMember(r.Id,Canonical(r.Scenario),[r.Id]))));
    }
    public static TowerBalanceDefinition Balance(TowerBossDiscoveryDefinition d,TowerRefinementComparisonMember[] members,bool confirmation)
    {
        var s=d.Stages.Schedules.Values.Single();var seeds=confirmation?s.Confirmation:s.Selection;var c=d.Contexts.Single();
        Require(members.Length is >=2 and <=4 && members.Select(m=>m.Id).Distinct().Count()==members.Length,"Invalid measured family.");
        Require(members.All(m=>HarnessJson.Hash(m.Scenario)==HarnessJson.Hash(Canonical(m.Scenario)) && Context(m.Scenario)==Context(d.References[0].Scenario)),"Changed measured context/order.");
        var cohort=new TowerBalanceCohort("fixed-cohort",d.Budget,d.RequiredPartySize,c.Id,TowerBossDiscovery.EquipmentBudgetHash(c.CharacterTemplates),d.BudgetPurpose);
        var result=new TowerBalanceDefinition(1,confirmation?"comparison-confirmation":"comparison-screen",TowerBalanceEvaluator.IntervalPolicy,
            d.ContentHashes,d.SettingsHash,d.ExecutionHash,[cohort],members.Select(m=>new TowerBalanceCellDefinition(m.Id,cohort.Id,
                m.Origins.Any(Controls.Contains)?"reference":"generated",m.Scenario with { Seeds=seeds },seeds.Count)).ToArray(),
            d.ExcludedCombatSeeds.Concat(d.Generation.Seeds).Concat(s.Discovery).Concat(confirmation?s.Selection:s.Confirmation).Distinct().Order().ToArray(),members.Length*seeds.Count);
        TowerBalanceEvaluator.Validate(result);return result;
    }
    public static TowerSearchBenchmarkPair Pair(IReadOnlyList<TowerBalanceTrial> left,IReadOnlyList<TowerBalanceTrial> right)
    {
        Require(left.Count==32 && right.Count==32 && left.Select(t=>t.Seed).Distinct().Count()==32
            && left.Select(t=>t.Seed).SequenceEqual(right.Select(t=>t.Seed)) && left.Concat(right).All(t=>Enum.IsDefined(t.Outcome)),"Complete paired confirmation required.");
        var gains=left.Zip(right).Count(p=>p.First.Outcome==BattleOutcome.Victory && p.Second.Outcome!=BattleOutcome.Victory);
        var losses=left.Zip(right).Count(p=>p.Second.Outcome==BattleOutcome.Victory && p.First.Outcome!=BattleOutcome.Victory);
        var g=TowerBalanceEvaluator.Wilson(gains,32,20)!;var l=TowerBalanceEvaluator.Wilson(losses,32,20)!;
        return new(32,gains,losses,(gains-losses)/32d,g.Lower-l.Upper,g.Upper-l.Lower);
    }
    public static TowerRefinementComparisonQuality Assess(TowerBalanceDefinition d,TowerRefinementComparisonMember[] family,IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        TowerFeedbackBenchmark.RequireEvidence(d,evidence);var rows=evidence.ToDictionary(e=>e.CellId);
        var rates=rows.ToDictionary(p=>p.Key,p=>TowerBalanceEvaluator.Wilson(p.Value.Trials.Count(t=>t.Outcome==BattleOutcome.Victory),32,8)!);
        var pairs=new Dictionary<string,TowerSearchBenchmarkPair>();
        void Add(string a,string b) {
            var left=family.Single(m=>m.Origins.Contains(a));var right=family.Single(m=>m.Origins.Contains(b));
            pairs.Add(a+"-minus-"+b,left.Id==right.Id?new(32,0,0,0,0,0):Pair(rows[left.Id].Trials,rows[right.Id].Trials));
        }
        Add("discovery-refinement-finalist","baseline-finalist");
        foreach(var policy in Policies)foreach(var control in Controls)Add(policy+"-finalist",control);
        return new(rates,pairs,"discovery-refinement-finalist-minus-baseline-finalist",
            "Exploratory policy comparison, one deterministic discovery run per policy; refinement adapts only to its own discovery evidence, separate combat stages; generation label is not a structural restart; no reliability/adoption/optimality claim. Never reselect on confirmation.");
    }
}
