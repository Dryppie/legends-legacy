using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.Items.Equipments.Slots;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessGearProfileTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Gear API tests cannot fight.")).Activate();
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));
    internal static string Profiles => Path.Combine(Catalogs, "tower-gear-specialization-screen.json");
    public void Dispose() => guard.Dispose();

    [Theory]
    [InlineData(3, 4)] [InlineData(7, 5)] [InlineData(8, 4)]
    [InlineData(10, 6)] [InlineData(13, 7)] [InlineData(15, 10)]
    public async Task Profiles_preserve_order_resources_and_instances_across_party_sizes(int floor, int slots)
    {
        var settings = TowerBundle.ReadSettings(Root);
        var content = OfflineContent.ForTower(Root, settings);
        var original = TowerPartyProgression.Scenarios(Root, Catalogs, TowerPartyProgression.Budget(slots)).Single(s => s.FloorNumber == floor);
        var beforeHash = HarnessJson.Hash(original);
        var runner = new TowerBattleRunner(Root, content);
        foreach (var profile in TowerGearProfiles.Read(Profiles).Profiles)
        {
            var changed = TowerGearProfiles.Apply(original, profile, content);
            Assert.Equal(original.Seeds, changed.Seeds);
            Assert.Equal(HarnessJson.Hash(original with { Party = [] }), HarnessJson.Hash(changed with { Party = [] }));
            Assert.Equal(HarnessJson.Hash(changed), HarnessJson.Hash(TowerGearProfiles.Apply(changed, profile, content)));
            foreach (var (a, b) in original.Party.Zip(changed.Party))
            {
                Assert.Equal(HarnessJson.Hash(a.Build), HarnessJson.Hash(b.Build with {
                    Equipment = a.Build.Equipment, IdentityEquipment = a.Build.IdentityEquipment }));
                var first = content.CreateBuild(a.Build); var second = content.CreateBuild(b.Build);
                Assert.Equal(first.Character.Id, second.Character.Id);
                Assert.Equal(first.Equipment.Select(e => e.Id), second.Equipment.Select(e => e.Id));
                Assert.Equal(first.EquippedEssences.Select(e => e.Id), second.EquippedEssences.Select(e => e.Id));
                Assert.Equal(a.Build.EssenceIds, b.Build.EssenceIds);
                foreach (var (oldItem, newItem) in a.Build.Equipment.Zip(b.Build.Equipment))
                {
                    var oldDefinition = content.Equipment.Evaluator.GetDefinition(oldItem.DefinitionId);
                    var newDefinition = content.Equipment.Evaluator.GetDefinition(newItem.DefinitionId);
                    Assert.Equal(oldDefinition.ArchetypeId, newDefinition.ArchetypeId);
                    Assert.Equal(oldDefinition.Rarity, newDefinition.Rarity);
                    if (profile.PartySlots.Count == 0 || profile.PartySlots.Contains(a.PartySlot))
                    {
                        if (profile.Specializations.TryGetValue(oldItem.Slot, out var specialization))
                            Assert.Equal(specialization, newDefinition.SpecializationId);
                        else Assert.Equal(oldItem, newItem);
                    }
                    else Assert.Equal(oldItem, newItem);
                }
            }
            _ = await runner.PrepareAsync(runner.CreateInput(changed, changed.Seeds[0], settings.Threat, settings.CheckpointIntervalTicks));
        }
        Assert.Equal(beforeHash, HarnessJson.Hash(original));
    }

    [Theory]
    [InlineData("unknown-specialization")] [InlineData("unknown-slot")] [InlineData("missing-slot")]
    [InlineData("no-target")] [InlineData("duplicate-target")] [InlineData("styles")]
    public void Invalid_profiles_fail_before_changing_the_source(string fault)
    {
        var content = OfflineContent.ForTower(Root, TowerBundle.ReadSettings(Root));
        var scenario = TowerPartyProgression.Scenarios(Root, Catalogs, TowerPartyProgression.Budget(4)).First();
        var profile = TowerGearProfiles.Read(Profiles).Profiles[0];
        if (fault == "styles") scenario = scenario with { Party = scenario.Party.Select(p => p with {
            Build = p.Build with { Equipment = p.Build.Equipment.Select(e => e with { UseNativeStyle = true }).ToArray() } }).ToArray() };
        profile = fault switch {
            "unknown-specialization" => profile with { Specializations = new Dictionary<EquipmentSlotType, string> { [EquipmentSlotType.MainHand] = "missing" } },
            "unknown-slot" => profile with { Specializations = new Dictionary<EquipmentSlotType, string> { [(EquipmentSlotType)999] = "armor" } },
            "missing-slot" => profile with { Specializations = new Dictionary<EquipmentSlotType, string> { [EquipmentSlotType.OffHand] = "armor" } },
            "no-target" => profile with { PartySlots = [50] },
            "duplicate-target" => profile with { PartySlots = [1, 1] },
            _ => profile
        };
        var hash = HarnessJson.Hash(scenario);
        Assert.Throws<InvalidDataException>(() => TowerGearProfiles.Apply(scenario, profile, content));
        Assert.Equal(hash, HarnessJson.Hash(scenario));
    }

    internal static TowerProposalRacingPlan RealPlan()
    {
        var budget = TowerPartyProgression.Budget(10) with { PriorityFloor = 15 };
        var baseline = TowerPartyProgression.Scenarios(Root, Catalogs, budget).Single(s => s.FloorNumber == 15);
        var controls = HarnessJson.Read<TowerWholePartyHistory>(Path.Combine(Catalogs, TowerWholeParty.Fixture));
        var recipes = controls.Parties[10].Take(3).Select(p => TowerPartySelection.Apply(baseline, p.Builds, [])).ToArray();
        var settings = TowerBundle.ReadSettings(Root);
        var inventory = TowerBossInventory.CreateForTower(Root, settings);
        var source = BalanceHarnessAffinitySearchTests.Baseline();
        source = source with { Racing = source.Racing with { Scope = source.Racing.Scope with {
            AllowedEssences = inventory.Essences.Select(e => new BossDiscoveryEssence(e.Id, e.SourceMonsterId)).ToArray(), OwnedCopies = null } },
            Policy = TowerProposalPolicies.BenchmarkAffinityCreation(TowerDamageSourceAffinities.Create(inventory).Affinities.Select(a => a.Id)) };
        var scope = new LoadoutScope("gear-api-test", settings, ExecutionIdentity.Current(),
            TowerBundle.Files.ToDictionary(f => f, f => HarnessJson.FileHash(Path.Combine(Root, "Data", f))));
        return BalanceHarnessAffinityFloorEvaluationTests.Project(source, recipes, inventory, scope,
            Enumerable.Range(1000, 109).ToArray(), [], [], budget, 15, 2);
    }

    [Fact]
    public async Task Search_profile_applies_to_references_and_every_proposal_with_the_existing_gate()
    {
        var source = RealPlan(); var hash = HarnessJson.Hash(source);
        var profile = TowerGearProfiles.Select(TowerGearProfiles.Read(Profiles), "armor-and-health");
        var settings = TowerBundle.ReadSettings(Root);
        var plan = TowerAffinitySearch.WithGearProfile(source, profile, Root, settings);
        Assert.Equal(hash, HarnessJson.Hash(source));
        Assert.NotEqual(hash, HarnessJson.Hash(plan));
        Assert.Equal(HarnessJson.Hash(source.Policy), HarnessJson.Hash(plan.Policy));
        Assert.Equal(HarnessJson.Hash(source.Racing.Panels), HarnessJson.Hash(plan.Racing.Panels));
        Assert.Equal(HarnessJson.Hash(source.Racing.Scope.Starts), HarnessJson.Hash(plan.Racing.Scope.Starts));
        Assert.Equal(source.Racing.BenchmarkReferenceId, plan.Racing.BenchmarkReferenceId);
        Assert.Equal(source.Racing.Scope.OwnedCopies, plan.Racing.Scope.OwnedCopies);
        var context = plan.Racing.Scope.Contexts.Single();
        var equipmentHash = TowerBossDiscovery.EquipmentBudgetHash(context.CharacterTemplates);
        var runner = new TowerBattleRunner(Root, OfflineContent.ForTower(Root, settings));
        foreach (var reference in plan.Racing.Scope.References)
        {
            Assert.Equal(equipmentHash, TowerBossDiscovery.EquipmentBudgetHash(reference.Scenario.Party));
            Assert.Contains("previous strength claims do not transfer", reference.Source);
            var scenario = reference.Scenario with { Seeds = [1001] };
            _ = await runner.PrepareAsync(runner.CreateInput(scenario, 1001, settings.Threat, settings.CheckpointIntervalTicks));
        }
        var count = 0;
        var report = await TowerProposalPolicies.RunAsync(plan, (request, _) => {
            count++;
            Assert.Equal(equipmentHash, TowerBossDiscovery.EquipmentBudgetHash(request.Scenario.Party));
            return Task.FromResult(new TowerPanelOutcome(HarnessJson.Hash(request), $"trial-{request.Ordinal:D6}", request.Seed,
                BattleOutcome.Defeat, 50, 0, 100));
        });
        Assert.Equal(528, count);
        Assert.Equal("BenchmarkRetained", TowerAffinitySearch.Summarize(plan, report).Status);
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerProposalPolicies.ReconstructAsync(plan, report)));
        Assert.Throws<InvalidDataException>(() => TowerAffinitySearch.WithGearProfile(source with {
            Racing = source.Racing with { Scope = source.Racing.Scope with { SettingsHash = new string('a', 64) } } }, profile, Root, settings));
    }

    [Fact]
    public async Task Commands_export_real_inputs_without_combat_and_refuse_overwrites()
    {
        var directory = Path.Combine(Path.GetTempPath(), "gear-api-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var input = Path.Combine(directory, "plan.json"); var output = Path.Combine(directory, "gear-plan.json");
            HarnessJson.WriteNew(input, RealPlan());
            string[] args = ["tower-affinity-search-gear", input, Profiles, "armor-and-health", Root, output];
            Assert.Equal(0, await BalanceHarness.Program.Main(args));
            Assert.Equal(0, await BalanceHarness.Program.Main(["tower-affinity-search-check", output]));
            Assert.NotEqual(0, await BalanceHarness.Program.Main(args));
            var scenarioPath = Path.Combine(directory, "scenario.json"); var changed = Path.Combine(directory, "changed.json");
            HarnessJson.WriteNew(scenarioPath, HarnessJson.Read<TowerProposalRacingPlan>(input).Racing.Scope.References[0].Scenario);
            Assert.Equal(0, await BalanceHarness.Program.Main(["tower-gear-profile-apply", scenarioPath, Profiles, "armor-and-health", Root, changed]));
            Assert.Equal(HarnessJson.Hash(HarnessJson.Read<TowerProposalRacingPlan>(output).Racing.Scope.References[0].Scenario),
                HarnessJson.Hash(HarnessJson.Read<TowerScenario>(changed)));
        }
        finally { Directory.Delete(directory, true); }
    }
}
