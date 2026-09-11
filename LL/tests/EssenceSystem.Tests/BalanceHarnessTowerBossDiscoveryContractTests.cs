using System.Text.Json;
using BalanceHarness;
using Domain.Models.Items;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTowerBossDiscoveryContractTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    internal static TowerScenario UserScenario => HarnessJson.Read<TowerScenario>(Path.GetFullPath(Path.Combine(Root,
        "../../../tools/BalanceHarness/Fixtures/tower-floor-1-user-party.json")));

    internal static TowerBossDiscoveryDefinition Definition(int floor = 1, int slots = 4, string purpose = "intended-progression")
    {
        var scenario = UserScenario;
        var budget = TowerPartyProgression.Budget(slots) with { PriorityFloor = floor };
        var content = HarnessJson.Read<JsonElement>(Path.Combine(Root, "Data/world-tower/tower-floors.json"));
        var count = content.GetProperty("floors").EnumerateArray().Single(f => f.GetProperty("floorNumber").GetInt32() == floor)
            .GetProperty("requiredSlots").GetInt32();
        var templates = Enumerable.Range(1, count).Select(slot => new TowerPartyRecipe(slot, scenario.Party[(slot - 1) % 5].Build with {
            Id = $"tower-discovery-character-{slot}", CharacterLevel = budget.CharacterLevel, Tier = budget.Tier,
            Rank = budget.Rank, Quality = budget.Quality, EssenceIds = [], IdentityEssenceIds = null })).ToArray();
        return TowerBossDiscovery.Create(Root, "independent-contract-test", budget, scenario.StartsAt,
            [new("fixed-equipment", templates)], 917312, [1234, 5678], budgetPurpose: purpose);
    }

    internal static BossBenchmarkReference Reference(string id = "user-reference") => new(id, "fixed-equipment",
        UserScenario with { Seeds = [] }, "User-authored benchmark; not a generator template", new string('a', 64));

    [Fact]
    public void Independent_inputs_and_schedules_do_not_depend_on_reference_registration_or_order()
    {
        var d = Definition();
        var reference = Reference();
        var second = reference with { Id = "second-reference", Scenario = reference.Scenario with {
            Party = reference.Scenario.Party.Select(p => p with { Build = p.Build with { EssenceIds = p.Build.EssenceIds.Reverse().ToArray() } }).ToArray() } };
        var withReferences = d with { References = [reference, second] };
        Assert.Equal(12624, TowerBossDiscovery.Validate(d).Total);
        Assert.Equal(14624, TowerBossDiscovery.Validate(Root, withReferences).Total);
        var inputs = TowerBossDiscovery.GenerationInputs(d);
        Assert.Equal(HarnessJson.Hash(inputs), HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(withReferences)));
        Assert.Equal(HarnessJson.Hash(inputs), HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(withReferences with { References = [second, reference] })));
        Assert.Equal(HarnessJson.Hash(d.Stages), HarnessJson.Hash(withReferences.Stages));
        Assert.DoesNotContain(reference.Source, JsonSerializer.Serialize(inputs, HarnessJson.Options));
        Assert.DoesNotContain("tower-discovery-character", JsonSerializer.Serialize(inputs, HarnessJson.Options));
        Assert.Equal(d.RequiredPartySize, inputs.EquipmentContexts["fixed-equipment"].Count);
        Assert.Equal(HarnessJson.Hash(d.ContentHashes), HarnessJson.Hash(inputs.ContentHashes));
        Assert.All(d.Contexts[0].CharacterTemplates, p => { Assert.Empty(p.Build.EssenceIds); Assert.Null(p.Build.IdentityEssenceIds); });
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { References = [reference, reference with { Id = "duplicate" }] }));
    }

    [Fact]
    public void Large_reference_family_fits_only_with_a_predeclared_sample_budget_and_keeps_the_hard_cap()
    {
        var d = Definition(); var reference = Reference();
        var references = Enumerable.Range(0, 97).Select(i => reference with {
            Id = $"historical-{i}", Scenario = reference.Scenario with {
                Party = reference.Scenario.Party.Select(p => p with { Build = p.Build with {
                    Id = $"historical-{i}-character-{p.PartySlot}" } }).ToArray() } }).ToArray();
        TowerBossDiscoveryDefinition Family(int count, int samples) => d with {
            References = references.Take(count).ToArray(), Stages = d.Stages with {
                Schedules = d.Stages.Schedules.ToDictionary(p => p.Key,
                    p => p.Value with { Confirmation = p.Value.Confirmation.Take(samples).ToArray() }) } };
        var fitting = Family(90, 972);
        Assert.Equal(99964, TowerBossDiscovery.Validate(Root, fitting).Total);
        Assert.Equal(HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(d)),
            HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(fitting)));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(Family(90, 1000)));
        Assert.True(TowerBossDiscovery.Validate(Family(96, 914)).Total <= 100000);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(Family(97, 1)));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(fitting with { MaximumBattles = 99963 }));
    }

    [Fact]
    public void Equivalent_prepared_recipes_are_deduplicated_despite_explicit_identity_and_equipment_order()
    {
        var d = Definition(); var reference = Reference();
        var equivalent = reference with { Id = "same-prepared-party", Scenario = reference.Scenario with {
            Party = reference.Scenario.Party.Select(p => p with { Build = p.Build with {
                Equipment = p.Build.Equipment.Reverse().ToArray(), IdentityEssenceIds = p.Build.EssenceIds } }).ToArray() } };
        TowerBossDiscovery.Validate(Root, d with { References = [equivalent] });
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { References = [reference, equivalent] }));
        var content = new OfflineContent(Root, new());
        Assert.Equal(reference.Scenario.Party.Select(p => content.CreateBuild(p.Build).Character.Id),
            equivalent.Scenario.Party.Select(p => content.CreateBuild(p.Build).Character.Id));
    }

    [Fact]
    public void Cost_counts_all_stages_contexts_references_and_reserves_at_exact_cap()
    {
        var d = Definition();
        var cost = TowerBossDiscovery.Validate(d);
        Assert.Equal(new BossDiscoveryCost(6144, 1024, 5000, 0, 256, 200, 12624), cost);
        Assert.Equal(cost, TowerBossDiscovery.Validate(d with { MaximumBattles = cost.Total }));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { MaximumBattles = cost.Total - 1 }));
        var second = d.Contexts[0] with { Id = "changed-gear", CharacterTemplates = d.Contexts[0].CharacterTemplates.Select(p =>
            p with { Build = p.Build with { AttributeRollMultiplier = 1.01 } }).ToArray() };
        var s = d.Stages.Schedules.Values.Single();
        var fresh = new BossDiscoverySchedule(Enumerable.Range(100,8).ToArray(),Enumerable.Range(200,64).ToArray(),
            Enumerable.Range(10000,1000).ToArray(),Enumerable.Range(300,32).ToArray());
        var expanded = d with { Contexts = [d.Contexts[0], second], Stages = d.Stages with {
            Schedules = new Dictionary<string, BossDiscoverySchedule> { [d.Contexts[0].Id] = s, [second.Id] = fresh } } };
        var doubled = TowerBossDiscovery.Validate(expanded);
        Assert.Equal(2 * (cost.Total - cost.ReplayReserve) + cost.ReplayReserve, doubled.Total);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(expanded with {
            Contexts = [d.Contexts[0], d.Contexts[0] with { Id = second.Id }] }));
    }

    [Theory]
    [InlineData(1,4)] [InlineData(5,5)] [InlineData(10,6)] [InlineData(11,7)] [InlineData(15,10)]
    public void Actual_full_party_is_mutable_at_fixed_gear_and_neutral_identity(int floor, int slots)
    {
        var d = Definition(floor, slots);
        Assert.True(TowerBossDiscovery.Validate(Root,d).Total > 0);
        var ids = d.AllowedEssences.GroupBy(e => e.Family, StringComparer.OrdinalIgnoreCase).Take(slots).Select(g => g.First().Id).ToArray();
        var party = TowerPartySelection.Choice("test", Enumerable.Range(1,d.RequiredPartySize).ToDictionary(i => i, _ => (IReadOnlyList<string>)ids));
        TowerBossDiscovery.ValidateParty(d,party); // Repetition across characters is legal.
        var scenario = TowerBossDiscovery.Scenario(d,d.Contexts[0].Id,party,[99]);
        var changed = TowerPartySelection.Choice("test",party.Builds.ToDictionary(p => p.Key,p =>
            p.Key == d.RequiredPartySize ? (IReadOnlyList<string>)p.Value.Reverse().ToArray() : p.Value));
        var alternate = TowerBossDiscovery.Scenario(d,d.Contexts[0].Id,changed,[99]);
        var content = new OfflineContent(Root,new());
        var before = new TowerBattleRunner(Root,content).CreateInput(scenario,99,new(),10);
        var after = new TowerBattleRunner(Root,content).CreateInput(alternate,99,new(),10);
        Assert.Equal(d.RequiredPartySize,before.Party.Count);
        Assert.Equal(before.Party.Select(p => p.Character.Id),after.Party.Select(p => p.Character.Id));
        Assert.Equal(TowerBossDiscovery.EquipmentBudgetHash(scenario.Party),TowerBossDiscovery.EquipmentBudgetHash(alternate.Party));
        Assert.NotEqual(HarnessJson.Hash(scenario),HarnessJson.Hash(alternate));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(Root,d with { RequiredPartySize = d.RequiredPartySize + 1 }));
    }

    [Theory]
    [InlineData(1,5)] [InlineData(5,4)] [InlineData(10,5)] [InlineData(11,4)] [InlineData(11,6)]
    public void Checkpoint_mismatches_require_explicit_diagnostic_scope_without_restricting_legal_combat(int floor, int slots)
    {
        var diagnostic = Definition(floor, slots, "diagnostic");
        Assert.True(TowerBossDiscovery.Validate(Root, diagnostic).Total > 0);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(diagnostic with { BudgetPurpose = "intended-progression" }));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(diagnostic with { BudgetPurpose = "unknown" }));
    }

    [Fact]
    public void Party_legality_enforces_families_and_owned_copies_without_party_wide_uniqueness()
    {
        var d = Definition();
        var party = TowerPartySelection.Choice("test",UserScenario.Party.ToDictionary(p => p.PartySlot,p => p.Build.EssenceIds));
        TowerBossDiscovery.ValidateParty(d,party);
        var copies = party.Builds.Values.SelectMany(ids => ids).GroupBy(id => id).ToDictionary(g => g.Key,g => g.Count());
        var owned = d with { OwnedCopies = copies };
        TowerBossDiscovery.Validate(owned);
        TowerBossDiscovery.ValidateParty(owned,party);
        var reduced = new Dictionary<string,int>(copies) { ["essence.illusion_fox"] = 4 };
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateParty(d with { OwnedCopies = reduced },party));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { OwnedCopies = new Dictionary<string,int>() }));
        var duplicate = party.Builds.ToDictionary(p => p.Key,p => p.Value);
        duplicate[1] = [party.Builds[1][0],party.Builds[1][0],party.Builds[1][2],party.Builds[1][3]];
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateParty(d,TowerPartySelection.Choice("test",duplicate)));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateParty(d,party with { Id = new string('f',64) }));
    }

    [Fact]
    public void Improve_mode_requires_exact_reference_start_and_never_exposes_independent_inputs()
    {
        var d = Definition(); var reference = Reference();
        var party = TowerPartySelection.Choice("reference",reference.Scenario.Party.ToDictionary(p => p.PartySlot,p => p.Build.EssenceIds));
        var start = new BossDiscoveryStart("supplied",reference.Id,party);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { References=[reference],Starts=[start] }));
        var improve = d with { Mode=TowerBossDiscovery.Improve,References=[reference],Starts=[start] };
        Assert.True(TowerBossDiscovery.Validate(improve).Total>0);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.GenerationInputs(improve));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(improve with { Starts=[start with { ReferenceId="unknown" }] }));
    }

    [Fact]
    public void Frozen_stages_budget_identity_and_content_cannot_silently_change()
    {
        var d=Definition(); var schedule=d.Stages.Schedules[d.Contexts[0].Id];
        var invalid = new[] {
            d with { SchemaVersion=2 }, d with { Generation=d.Generation with { CandidatesPerArm=1001 } },
            d with { Generation=d.Generation with { Objective="closest-to-30-percent" } },
            d with { ExcludedCombatSeeds=[schedule.Confirmation[0]] },
            d with { Stages=d.Stages with { Schedules=new Dictionary<string,BossDiscoverySchedule> {
                [d.Contexts[0].Id]=schedule with { Selection=schedule.Discovery } } } },
            d with { Budget=d.Budget with { CharacterLevel=1 } },
            d with { Contexts=[d.Contexts[0] with { CharacterTemplates=UserScenario.Party }] },
            d with { References=[Reference() with { EvidenceHash="not-a-hash" }] },
        };
        Assert.All(invalid,changed => Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(changed)));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(Root,d with { ExecutionHash=new string('b',64) }));
        using var temp=new DiscoveryTemp(); var path=Path.Combine(temp.Path,"definition.json");
        HarnessJson.WriteNew(path,d);
        Assert.Equal(HarnessJson.Hash(d),HarnessJson.Hash(TowerBossDiscovery.Read(path)));
        Assert.Throws<JsonException>(() => TowerBossSearch.Read(path));
        File.WriteAllText(path,JsonSerializer.Serialize(d,HarnessJson.Options).TrimEnd().TrimEnd('}')+",\"anchor\":\"user-party\"}");
        Assert.Throws<JsonException>(() => TowerBossDiscovery.Read(path));
    }

    [Fact]
    public void Reference_ancestry_survives_mutation_and_recombination_and_cannot_be_laundered()
    {
        var d=Definition();var reference=Reference();
        var party=TowerPartySelection.Choice("reference",reference.Scenario.Party.ToDictionary(p=>p.PartySlot,p=>p.Build.EssenceIds));
        var improve=d with { Mode=TowerBossDiscovery.Improve,References=[reference],Starts=[new("supplied",reference.Id,party)] };
        var fresh=new BossDiscoveryProvenance("fresh",d.Generation.Seeds[0],"random","fresh-random",[],[]);
        var mutation=new BossDiscoveryProvenance("mutated",d.Generation.Seeds[0],"constructive-joint","single",["supplied"],[reference.Id]);
        var child=new BossDiscoveryProvenance("child",d.Generation.Seeds[0],"constructive-joint","recombine",["mutated","fresh"],[reference.Id]);
        TowerBossDiscovery.ValidateProvenance(improve,[fresh,mutation,child]);
        Assert.Throws<InvalidDataException>(()=>TowerBossDiscovery.ValidateProvenance(improve,[fresh,mutation,child with { ReferenceIds=[] }]));
        Assert.Throws<InvalidDataException>(()=>TowerBossDiscovery.ValidateProvenance(improve,[fresh,child,mutation]));
        Assert.Throws<InvalidDataException>(()=>TowerBossDiscovery.ValidateProvenance(d,[mutation]));
        TowerBossDiscovery.ValidateProvenance(d with { References=[reference] },[fresh]);
    }

    [Fact]
    public async Task Preparation_CLI_validates_without_running_any_combat()
    {
        using var temp=new DiscoveryTemp();var path=Path.Combine(temp.Path,"definition.json");
        HarnessJson.WriteNew(path,Definition());
        var output=Path.Combine(temp.Path,"prepared");
        Assert.Equal(0,await BalanceHarness.Program.Main(["tower-boss-discovery-prepare","--definition",path,"--output",output,"--content-root",Root]));
        Assert.Equal(12624,HarnessJson.Read<BossDiscoveryCost>(Path.Combine(output,"cost.json")).Total);
        Assert.True(File.Exists(Path.Combine(output,"generation-inputs.json")));
        Assert.False(Directory.Exists(Path.Combine(output,"battles")));
    }
}

internal sealed class DiscoveryTemp : IDisposable
{
    public string Path { get; }=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"tower-discovery-"+Guid.NewGuid().ToString("N"));
    public DiscoveryTemp() => Directory.CreateDirectory(Path);
    public void Dispose() => Directory.Delete(Path,true);
}
