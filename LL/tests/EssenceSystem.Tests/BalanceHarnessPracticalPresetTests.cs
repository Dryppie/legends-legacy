using BalanceHarness;
using BalanceHarness.ProcessFixture;
using I = EssenceSystem.Tests.BalanceHarnessIncumbentSelectionTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessPracticalPresetTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-practical-preset-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preset fixture entered combat.")).Activate();
    private sealed record Input(TowerPracticalRequest Request, string RequestPath, string Output, TowerBossDiscoveryDefinition Template);
    public BalanceHarnessPracticalPresetTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    private Input Fixture(Func<TowerBossDiscoveryDefinition, TowerBossDiscoveryDefinition>? change = null)
    {
        var registry = Path.Combine(root, "history"); Directory.CreateDirectory(registry);
        var content = Path.Combine(root, "content"); Directory.CreateDirectory(content);
        var prior = Path.Combine(registry, "prior-seed-ledger.json"); HarnessJson.WriteNew(prior, new { historical = new[] { -987 } });
        var d = BalanceHarnessPracticalAllocationTests.Template(I.Definition() with { ExcludedCombatSeeds = [-987] });
        if (change is not null) d = change(d);
        var templatePath = Path.Combine(root, "source-template.json"); HarnessJson.WriteNew(templatePath, d);
        var q = new TowerPracticalRequest(TowerPracticalSearch.AllocationVersion, content, templatePath, HarnessJson.FileHash(templatePath),
            registry, Path.Combine(registry, "future-run"), new Dictionary<string, string> { [prior] = HarnessJson.FileHash(prior) },
            300, 32 * 1048576, 5, 128, new Dictionary<string, string>(), new Dictionary<string, string>(),
            new(19, "literal-preset-fixture", 8, 32, 256));
        var requestPath = Path.Combine(root, "source-request.json"); HarnessJson.WriteNew(requestPath, q);
        return new(q, requestPath, Path.Combine(root, "preset"), d);
    }

    [Theory]
    [InlineData("anchor-0")]
    [InlineData("anchor-1")]
    public async Task Command_publishes_only_new_inputs_with_an_explicit_designation_and_preserves_all_other_fields(string primary)
    {
        var i = Fixture(); var requestHash = HarnessJson.FileHash(i.RequestPath);
        var definitionHash = HarnessJson.Hash(i.Template);
        Assert.Equal(0, await BalanceHarness.Program.Main(["tower-practical-search-incumbent-tie-preset", i.RequestPath, primary, i.Output]));
        var receipt = HarnessJson.Read<TowerPracticalPresetReceipt>(Path.Combine(i.Output, "preset.json"));
        var q = HarnessJson.Read<TowerPracticalRequest>(receipt.RequestPath);
        var d = TowerBossDiscovery.Read(receipt.TemplatePath);
        Assert.Equal("PreparedNeedsAdmission", receipt.Status); Assert.True(receipt.AdmissionRequired);
        Assert.Equal(TowerPracticalSearch.IncumbentTiePresetVersion, receipt.Version);
        Assert.Equal(0, receipt.NewValues); Assert.Equal(0, receipt.Fights);
        Assert.Equal(primary, receipt.SelectionPrimaryReferenceId);
        Assert.Equal(i.Template.Starts.Single(s => s.ReferenceId == primary).Party.Id, receipt.SelectionPrimaryPartyId);
        Assert.Equal(TowerBossStudyPolicy.IncumbentTieVersion, d.Stages.SelectionPolicyVersion);
        Assert.Equal(primary, d.Stages.SelectionPrimaryReferenceId);
        Assert.Equal(definitionHash, HarnessJson.Hash(d with { Stages = i.Template.Stages }));
        Assert.Equal(HarnessJson.Hash(i.Request), HarnessJson.Hash(q with {
            DefinitionPath = i.Request.DefinitionPath, DefinitionHash = i.Request.DefinitionHash }));
        Assert.Equal(requestHash, receipt.SourceRequestFileHash); Assert.Equal(requestHash, HarnessJson.FileHash(i.RequestPath));
        Assert.Equal(i.Request.DefinitionHash, receipt.SourceTemplateFileHash);
        Assert.Equal(i.Request.DefinitionHash, HarnessJson.FileHash(i.Request.DefinitionPath));
        Assert.Equal(receipt.TemplateFileHash, q.DefinitionHash); Assert.Equal(q.DefinitionHash, HarnessJson.FileHash(q.DefinitionPath));
        Assert.Equal(receipt.RequestFileHash, HarnessJson.FileHash(receipt.RequestPath));
        Assert.Equal(TowerBossDiscovery.Validate(TowerPracticalSearch.ValidateAllocationTemplate(i.Request, i.Template)), receipt.Cost);
        Assert.Empty(d.Generation.Seeds);
        Assert.All(d.Stages.Schedules.Values, s => Assert.Empty(s.Discovery.Concat(s.Selection).Concat(s.Confirmation).Concat(s.Diagnostics)));
        Assert.Equal(new[] { "preset.json", "request.json", "template.json" }, Directory.GetFiles(i.Output).Select(Path.GetFileName).Order());
        Assert.False(Path.Exists(q.OutputRoot));
        Assert.Single(Directory.GetFiles(q.RegistryRoot, "*", SearchOption.AllDirectories));
        Assert.Equal(TowerBossStudyPolicy.ZeroWinVersion, I.Definition().Stages.SelectionPolicyVersion);
        Assert.Null(I.Definition().Stages.SelectionPrimaryReferenceId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("unknown")]
    [InlineData("ANCHOR-0")]
    [InlineData(" anchor-0 ")]
    [InlineData("party-id")]
    public void Missing_or_inexact_reference_never_infers_a_primary_or_writes_output(string primary)
    {
        var i = Fixture();
        if (primary == "party-id") primary = i.Template.Starts[0].Party.Id;
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.CreateIncumbentTiePreset(i.RequestPath, primary, i.Output));
        Assert.False(Path.Exists(i.Output)); Assert.False(Path.Exists(i.Request.OutputRoot));
    }

    [Theory]
    [InlineData("construction")]
    [InlineData("selection")]
    [InlineData("feedback")]
    [InlineData("reference")]
    [InlineData("history")]
    [InlineData("independent")]
    [InlineData("racing")]
    [InlineData("anchored")]
    [InlineData("duplicate-start")]
    public void Preset_cannot_relabel_scheduled_or_unsupported_inputs(string invalid)
    {
        var i = Fixture(d => invalid switch {
            "construction" => d with { Generation = d.Generation with { Seeds = [17] } },
            "selection" => d with { Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value with { Selection = [201] }) } },
            "feedback" => d with { Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value with { Feedback = [] }) } },
            "reference" => d with { References = d.References.Select(r => r with { Scenario = r.Scenario with { Seeds = [301] } }).ToArray() },
            "history" => d with { ExcludedCombatSeeds = [-987, -987] },
            "independent" => d with { Mode = TowerBossDiscovery.Independent, Starts = [] },
            "racing" => d with { Generation = d.Generation with { PolicyVersion = TowerEvaluationAllocationSearch.Version } },
            "anchored" => d with { Generation = d.Generation with { PolicyVersion = TowerAnchoredNeighborhoodSearch.Version } },
            _ => d with { Starts = [d.Starts[0], d.Starts[0] with { Id = "duplicate" }] }
        });
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.CreateIncumbentTiePreset(i.RequestPath, "anchor-0", i.Output));
        Assert.False(Path.Exists(i.Output)); Assert.False(Path.Exists(i.Request.OutputRoot));
    }

    [Fact]
    public void Existing_designation_is_idempotent_but_cannot_be_switched()
    {
        var i = Fixture(d => d with { Stages = d.Stages with {
            SelectionPolicyVersion = TowerBossStudyPolicy.IncumbentTieVersion, SelectionPrimaryReferenceId = "anchor-1" } });
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.CreateIncumbentTiePreset(i.RequestPath, "anchor-0", i.Output));
        Assert.False(Path.Exists(i.Output));
        var receipt = TowerPracticalSearch.CreateIncumbentTiePreset(i.RequestPath, "anchor-1", i.Output);
        Assert.Equal(HarnessJson.Hash(i.Template), HarnessJson.Hash(TowerBossDiscovery.Read(receipt.TemplatePath)));
    }

    [Theory]
    [InlineData("changed-template")]
    [InlineData("existing-run")]
    [InlineData("existing-preset")]
    [InlineData("in-history")]
    [InlineData("in-content")]
    [InlineData("declared-request")]
    public void Publication_rejects_changed_pins_existing_outputs_and_wrong_routes(string invalid)
    {
        var i = Fixture(); var output = i.Output;
        if (invalid == "changed-template") File.AppendAllText(i.Request.DefinitionPath, " ");
        if (invalid == "existing-run") Directory.CreateDirectory(i.Request.OutputRoot);
        if (invalid == "existing-preset") { Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "keep.txt"), "preserve"); }
        if (invalid == "in-history") output = Path.Combine(i.Request.RegistryRoot, "preset");
        if (invalid == "in-content") output = Path.Combine(i.Request.ContentRoot, "preset");
        if (invalid == "declared-request")
        {
            var path = Path.Combine(root, "declared.json");
            HarnessJson.WriteNew(path, i.Request with { Version = TowerPracticalSearch.Version, Allocation = null });
            i = i with { RequestPath = path };
        }
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.CreateIncumbentTiePreset(i.RequestPath, "anchor-0", output));
        if (invalid == "existing-preset") Assert.Equal("preserve", File.ReadAllText(Path.Combine(output, "keep.txt")));
        else Assert.False(Path.Exists(output));
    }

    [Fact]
    public void Cancellation_before_preparation_creates_no_artifacts()
    {
        var i = Fixture(); using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.Throws<OperationCanceledException>(() => TowerPracticalSearch.CreateIncumbentTiePreset(i.RequestPath, "anchor-0", i.Output, stop.Token));
        Assert.False(Path.Exists(i.Output)); Assert.False(Path.Exists(i.Request.OutputRoot));
    }

    [Fact]
    public async Task Prepared_request_flows_through_literal_allocation_real_selection_and_binding_verification()
    {
        var i = Fixture(); var receipt = TowerPracticalSearch.CreateIncumbentTiePreset(i.RequestPath, "anchor-1", i.Output);
        var q = HarnessJson.Read<TowerPracticalRequest>(receipt.RequestPath); var d = TowerBossDiscovery.Read(receipt.TemplatePath);
        var history = TowerRefinementComparisonLaunch.Refresh(q.RegistryRoot, q.OutputRoot, q.RequiredHistory, [-987], default);
        Directory.CreateDirectory(q.OutputRoot); HarnessJson.WriteNew(Path.Combine(q.OutputRoot, "request.json"), q);
        var bound = TowerPracticalSearch.AllocateAndRegister(q, new(d, history), default, () => { }, candidate: FixtureHost.AllocationCandidate).Definition;
        Assert.Equal("anchor-1", bound.Stages.SelectionPrimaryReferenceId);
        TowerPracticalSearch.VerifyAllocation(q.OutputRoot, q, bound, default, FixtureHost.AllocationCandidate);
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.VerifyAllocation(q.OutputRoot, q,
            bound with { Stages = bound.Stages with { SelectionPrimaryReferenceId = "anchor-0" } }, default, FixtureHost.AllocationCandidate));
        var report = await BalanceHarnessPracticalSearchTests.Study(bound, "incumbent");
        Assert.Equal("Complete", report.Status);
        Assert.Equal(bound.Starts[1].Party.Id, report.Confirmation!.Members.Single(m => m.Primary).GeneratedIds.Single());
        Assert.Equal("IncumbentRetained", TowerPracticalSearch.Assess(bound, report, new string('a', 64)).StrengthDecision);
    }
}
