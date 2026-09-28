using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat.Abilities;
using Services.LL.Items;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerBalanceSelectionTests : IDisposable
{
    private static string Root => TestContentPaths.FindApiRoot();
    private readonly string temp = Path.Combine(Path.GetTempPath(), "tower-balance-selection-" + Guid.NewGuid().ToString("N"));
    public BalanceHarnessTowerBalanceSelectionTests() => Directory.CreateDirectory(temp);
    public void Dispose() => Directory.Delete(temp, true);
    private static TowerScenario Scenario => HarnessJson.Read<TowerScenario>(Path.GetFullPath(Path.Combine(Root,
        "../../../tools/BalanceHarness/Fixtures/tower-floor-1.json"))) with { Seeds = [1337] };

    [Theory]
    [InlineData(17, 1, null)]
    [InlineData(18, 2, null)]
    [InlineData(18, 3, "healing-v1")]
    [InlineData(18, 4, "healing-v1")]
    public void Saved_settings_preserve_independent_rules_equipment_and_ability_selection(int rules, int release, string? profile)
    {
        var settings = new TowerSettings(new(), 10, new(rules, release, profile));
        TowerBundle.WriteSettings(Path.Combine(temp, "appsettings.json"), settings);
        Assert.Equal(HarnessJson.Hash(settings), HarnessJson.Hash(TowerBundle.ReadSettings(temp)));
        TowerBundle.CopyContent(Root, temp, default);
        var content = OfflineContent.ForTower(temp, settings);
        var production = JsonStarterEquipmentCatalog.Load(Path.Combine(Root, "Data/equipment/equipment-starters.v1.json"), release);
        Assert.Equal(rules, content.AttributeRulesVersion);
        Assert.Equal(release, content.Equipment.Evaluator.Balance.Version);
        Assert.Equal(HarnessJson.Hash(production.GetOptions(1)), HarnessJson.Hash(content.Equipment.GetOptions(1)));
        var input = new TowerBattleRunner(temp, content).CreateInput(Scenario, 1337, settings.Threat, 10);
        Assert.Equal(settings.Balance, input.Balance);
        Assert.All(input.Party, p => Assert.Equal(rules, TowerBattleRunner.ToSnapshot(p.Character, content).AttributeRulesVersion));
    }

    [Fact]
    public void Historical_settings_and_inputs_keep_their_original_serialization_and_implicit_catalog()
    {
        var settings = new TowerSettings(new(), 10);
        TowerBundle.WriteSettings(Path.Combine(temp, "appsettings.json"), settings);
        var captured = TowerBundle.ReadSettings(temp);
        Assert.Null(captured.Balance);
        Assert.Equal(HarnessJson.Hash(new { settings.Threat, settings.CheckpointIntervalTicks }), HarnessJson.Hash(captured));
        var old = new TowerBattleRunner(Root, new OfflineContent(Root, settings.Threat)).CreateInput(Scenario, 1337, settings.Threat, 10);
        var restored = new TowerBattleRunner(Root, OfflineContent.ForTower(Root, captured)).CreateInput(Scenario, 1337, settings.Threat, 10);
        Assert.Equal(HarnessJson.Hash(old), HarnessJson.Hash(restored));
        Assert.DoesNotContain("\"balance\"", JsonSerializer.Serialize(restored, HarnessJson.Options));
    }

    [Fact]
    public async Task Search_archive_and_replay_use_the_selected_profile_and_reject_changed_settings()
    {
        var settings = TowerBundle.ReadSettings(Root);
        Assert.Equal(new TowerBalanceSelection(18, 4, "healing-v1"), settings.Balance);
        var scope = new LoadoutScope("selection-test", settings, ExecutionIdentity.Current(),
            TowerBundle.CopyContent(Root, Path.Combine(temp, "content"), default));
        HarnessJson.WriteNew(Path.Combine(temp, "scope.json"), scope);
        Directory.CreateDirectory(Path.Combine(temp, "recipes"));
        Directory.CreateDirectory(Path.Combine(temp, "battles"));
        var archive = new TowerLoadoutArchive(temp, scope, 1);
        var (trial, report) = await archive.EvaluateAsync("test", "test", Scenario, 1337, default);
        HarnessJson.WriteNew(Path.Combine(temp, "files.json"), Directory.EnumerateFiles(temp, "*", SearchOption.AllDirectories)
            .ToDictionary(f => Path.GetRelativePath(temp, f).Replace('\\', '/'), HarnessJson.FileHash));
        var replay = await TowerLoadoutArchive.ReplayAsync(temp, trial.Id, false);
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(replay));
        var input = archive.Materialize(Scenario, 1337);
        var altered = scope with { Settings = settings with { Balance = settings.Balance! with { AbilityBalanceProfile = null } } };
        Assert.NotEqual(TowerLoadoutArchive.Key(scope, "test", input), TowerLoadoutArchive.Key(altered, "test", input));
        var wrongRunner = new TowerBattleRunner(Root, OfflineContent.ForTower(Root, altered.Settings));
        await Assert.ThrowsAsync<InvalidDataException>(() => wrongRunner.PrepareAsync(input));
    }

    [Fact]
    public void Search_mechanics_include_the_effective_healing_override_and_its_hash()
    {
        var settings = TowerBundle.ReadSettings(Root);
        var inventory = TowerBossInventory.CreateForTower(Root, settings);
        const string file = "combat/ability-balance.healing-v1.json";
        Assert.Equal(HarnessJson.FileHash(Path.Combine(Root, "Data", file)), inventory.SourceHashes[file]);
        Assert.Equal(1.05f, Coefficient("ability.creature.lizardfolk_shaman.herb_mixture"));
        Assert.Equal(1.25f, Coefficient("ability.creature.treant_sapling.sprouting_surge"));
        Assert.NotEmpty(TowerDamageSourceAffinities.Create(inventory).Affinities);
        float Coefficient(string id) => inventory.Nodes.Single(n => n.Kind == TowerMechanicNodeKind.Ability && n.Id == id)
            .Definition.Deserialize<AbilitySpec>(HarnessJson.Options)!.Effects[0].ScalingCoefficient;
    }

    [Theory]
    [InlineData("equipment/equipment-releases.json")]
    [InlineData("equipment/equipment-starters.v4.json")]
    [InlineData("equipment/equipment-sets.v4.json")]
    [InlineData("combat/ability-balance.healing-v1.json")]
    public async Task Normal_archives_authenticate_selected_release_content(string file)
    {
        var scenario = Path.Combine(temp, "scenario.json"); HarnessJson.WriteNew(scenario, Scenario);
        var output = Path.Combine(temp, "run");
        await TowerBundle.CreateAsync(Root, scenario, output);
        var saved = TowerBundle.ReadSaved(output);
        Assert.Equal(TowerBundle.ReadSettings(Root).Balance, saved.Inputs[0].Balance);
        Assert.Contains(file, saved.Manifest.ContentHashes.Keys);
        File.AppendAllText(Path.Combine(output, "content", "Data", file), " ");
        Assert.Throws<InvalidDataException>(() => TowerBundle.ReadSaved(output));
    }

    [Fact]
    public async Task Supported_racing_and_proposal_validation_bind_the_profile_hash_without_changing_the_policy()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Synthetic policy check cannot fight.")).Activate();
        var plan = BalanceHarnessAffinitySearchTests.Baseline();
        var hashes = TowerBundle.Files.ToDictionary(f => f, _ => new string('a', 64));
        const string profile = "combat/ability-balance.healing-v1.json";
        hashes[profile] = new string('b', 64);
        var sources = plan.Racing.Mechanics.SourceHashes.ToDictionary(); sources.Add(profile, hashes[profile]);
        plan = plan with { Racing = plan.Racing with {
            Scope = plan.Racing.Scope with { ContentHashes = hashes },
            Mechanics = plan.Racing.Mechanics with { SourceHashes = sources } },
            DamageAffinityInventory = plan.DamageAffinityInventory! with { SourceHashes = sources } };
        TowerAffinitySearch.Validate(plan);
        var report = await TowerProposalPolicies.RunAsync(plan, (request, _) => Task.FromResult(new TowerPanelOutcome(
            HarnessJson.Hash(request), "literal-" + request.Ordinal, request.Seed, Domain.Models.Combat.BattleOutcome.Defeat, 50, 50, 1)));
        Assert.Equal(528, report.Evaluation.ChargedEvaluations);
        Assert.Equal("Complete", report.Evaluation.Status);
        hashes[profile] = new string('c', 64);
        Assert.Throws<InvalidDataException>(() => TowerAffinitySearch.Validate(plan));
    }
}
