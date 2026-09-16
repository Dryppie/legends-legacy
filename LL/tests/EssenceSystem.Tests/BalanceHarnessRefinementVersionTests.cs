using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessRefinementLaunchFixture;
using Runtime = EssenceSystem.Tests.BalanceHarnessRefinementComparisonFixture;
using Model = BalanceHarness.TowerRefinementComparisonModel;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessRefinementVersionTests
{
    static F Novel() => new(TowerSharedExecutable.Profile, Model.NovelVersion);
    static TowerBossDiscoveryDefinition[] Pair(string? version)
    {
        var source = Runtime.Definitions()[0]; var schedule = source.Stages.Schedules.Values.Single();
        var seeds = new TowerRefinementComparisonSeeds(source.ExcludedCombatSeeds.ToArray(), source.Generation.Seeds.ToArray(),
            schedule.Discovery.ToArray(), schedule.Selection.ToArray(), schedule.Confirmation.ToArray());
        return Model.Policies.Select(p => Model.Definition(source, seeds, p, version)).ToArray();
    }

    [Fact] public void Omitted_version_preserves_legacy_serialized_request_and_definitions()
    {
        var f = new F(); var q = f.Request.Preflight;
        var oldShape = new { q.ContentRoot, q.DefinitionPath, q.CampaignPath, q.LedgerPath, q.RegistrySnapshotPath,
            q.DriverReceiptPath, q.ExpectedReservations, q.Pins };
        Assert.Equal(JsonSerializer.Serialize(oldShape, HarnessJson.Options), JsonSerializer.Serialize(q, HarnessJson.Options));
        Assert.Equal(HarnessJson.Hash(oldShape), HarnessJson.Hash(q));
        Assert.Equal(HarnessJson.Hash(Runtime.Definitions()), HarnessJson.Hash(Pair(null)));
        Assert.Equal(HarnessJson.Hash(Pair(Model.Version)), HarnessJson.Hash(Pair(null)));
        Assert.Equal(TowerDiscoveryRefinementSearch.Version, Pair(null)[1].Generation.PolicyVersion);
    }

    [Fact] public void Novel_pair_changes_only_comparison_identity_and_candidate_policy()
    {
        var old = Pair(null); var next = Pair(Model.NovelVersion); Model.ValidatePair(next);
        Assert.Equal(TowerDiscoveryRefinementSearch.NovelVersion, next[1].Generation.PolicyVersion);
        for (var i = 0; i < 2; i++) {
            Assert.Equal(Model.NovelVersion, next[i].Id);
            var normalized = next[i] with { Id = old[i].Id, Generation = next[i].Generation with { PolicyVersion = old[i].Generation.PolicyVersion } };
            Assert.Equal(HarnessJson.Hash(old[i]), HarnessJson.Hash(normalized));
        }
        Assert.Equal(HarnessJson.Hash(Model.Canonical(old[0].References[0].Scenario)), HarnessJson.Hash(Model.Canonical(next[0].References[0].Scenario)));
    }

    [Fact] public void Unknown_mixed_and_mismatched_policy_versions_are_rejected()
    {
        foreach (var bad in new[] { "", "unknown", TowerDiscoveryRefinementSearch.RoleSafeVersion })
            Assert.Throws<InvalidDataException>(() => Pair(bad));
        var pair = Pair(Model.NovelVersion); pair[0] = Pair(null)[0];
        Assert.Throws<InvalidDataException>(() => Model.ValidatePair(pair));
        pair = Pair(Model.NovelVersion); pair[1] = pair[1] with { Generation = pair[1].Generation with { PolicyVersion = TowerDiscoveryRefinementSearch.Version } };
        Assert.Throws<InvalidDataException>(() => Model.ValidatePair(pair));
        var f = new F(); Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Validate(
            f.Request with { Preflight = f.Request.Preflight with { ComparisonVersion = "unknown" } }));
        Assert.False(Directory.Exists(f.Request.StudyRoot));
    }

    [Fact] public void Novel_preflight_requires_a_version_specific_verified_driver_receipt()
    {
        var old = JsonSerializer.SerializeToElement(new { status = "VerifiedRefinementComparisonDriver", reservations = 1 });
        TowerRefinementComparisonPreflight.VerifyDriverReceipt(old, 1, Model.Version);
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonPreflight.VerifyDriverReceipt(old, 1, Model.NovelVersion));
        foreach (var policy in new[] { TowerDiscoveryRefinementSearch.Version, TowerDiscoveryRefinementSearch.NovelVersion }) {
            var next = JsonSerializer.SerializeToElement(new { status = "VerifiedRefinementComparisonDriver", reservations = 1,
                comparisonVersion = Model.NovelVersion, refinementPolicy = policy });
            if (policy == TowerDiscoveryRefinementSearch.NovelVersion) {
                TowerRefinementComparisonPreflight.VerifyDriverReceipt(next, 1, Model.NovelVersion);
                Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonPreflight.VerifyDriverReceipt(next, 2, Model.NovelVersion));
            } else Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonPreflight.VerifyDriverReceipt(next, 1, Model.NovelVersion));
        }
    }

    [Fact] public void Version_change_invalidates_authorization_before_binding()
    {
        var f = new F(); var changed = f.Request with { Preflight = f.Request.Preflight with { ComparisonVersion = Model.NovelVersion } };
        Assert.NotEqual(HarnessJson.Hash(f.Request), HarnessJson.Hash(changed));
        var inspected = false;
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.BindCore(changed, f.Permit,
            _ => { inspected = true; return f.Inputs; }, (_, _) => { }, default, F.Candidate));
        Assert.False(inspected); Assert.False(Directory.Exists(f.Request.StudyRoot));
    }

    static void RejectPreflight(Func<TowerRefinementPreflightResult, TowerRefinementPreflightResult> change)
    {
        var f = Novel(); var allocated = false;
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.BindCore(f.Request, f.Permit,
            _ => f.Inputs with { Preflight = change(f.Inputs.Preflight) }, (_, _) => { }, default,
            (_, _) => { allocated = true; return 17; }));
        Assert.False(allocated); Assert.False(File.Exists(f.P("allocation-journal.jsonl")));
        Assert.False(File.Exists(f.P("history-input.json"))); Assert.True(File.Exists(f.P("binding-failure.json")));
    }

    [Fact] public void Preflight_policy_mismatch_is_rejected_before_any_fixture_label()
        => RejectPreflight(p => p with { Version = Model.Version });

    [Fact] public void Preflight_request_mismatch_is_rejected_before_any_fixture_label()
        => RejectPreflight(p => p with { RequestHash = new string('0', 64) });

    [Fact] public async Task Novel_binding_and_archive_reconstruction_preserve_schedules_and_charges()
    {
        var f = Novel(); f.Bind(); var definitions = HarnessJson.Read<TowerBossDiscoveryDefinition[]>(f.P("definitions.json"));
        Assert.Equal(HarnessJson.Hash(Pair(Model.NovelVersion)), HarnessJson.Hash(definitions));
        var quality = await f.Run(); var verified = await TowerRefinementComparisonRun.Reconstruct(f.P("run"), new Runtime());
        Assert.Equal(HarnessJson.Hash(quality), HarnessJson.Hash(verified));
        Assert.Equal(Model.NovelVersion, HarnessJson.Read<JsonElement>(f.P("run/started.json")).GetProperty("version").GetString());
        Assert.Equal(TowerDiscoveryComparisonGate.NovelVersion, HarnessJson.Read<TowerDiscoveryComparisonDecision>(f.P("run/discovery-gate.json")).Version);
        var seeds = HarnessJson.Read<TowerRefinementComparisonSeeds>(f.P("seeds.json"));
        Assert.Equal(new[] { 1, 4, 8, 32 }, new[] { seeds.Generation.Length, seeds.Discovery.Length, seeds.Selection.Length, seeds.Confirmation.Length });
        TowerRescreenAttempts.Verify(f.P("run/attempts.bin"), 240);
        TowerBulkCampaign.VerifyFiles(f.Request.StudyRoot, "launch-files.json", true, default);
        await Assert.ThrowsAsync<InvalidDataException>(() => f.Run());
    }

    [Fact] public async Task Incomplete_novel_discovery_preserves_charges_and_blocks_selection()
    {
        var f = Novel(); f.Bind(); var runtime = new Runtime("partial-candidate");
        await Assert.ThrowsAsync<InvalidDataException>(() => f.Run(execute: (p, d, s, b, ct) =>
            TowerRefinementComparisonRun.Execute(p, d, s, b, runtime, ct, f.Request.ArchiveProfile)));
        var gate = HarnessJson.Read<TowerDiscoveryComparisonDecision>(f.P("run/discovery-gate.json"));
        Assert.Equal(TowerDiscoveryComparisonGate.NovelVersion, gate.Version); Assert.Equal("StoppedDiscovery", gate.Status);
        Assert.Equal(TowerDiscoveryRefinementSearch.NovelVersion, gate.Arms[1].Policy);
        TowerRescreenAttempts.Verify(f.P("run/attempts.bin"), 120);
        Assert.False(File.Exists(f.P("run/nominations.json"))); Assert.False(Directory.Exists(f.P("run/selection")));
        Assert.DoesNotContain("selection", runtime.Calls);
    }

    [Fact] public void Legacy_candidate_nominations_cannot_enter_the_novel_comparison()
    {
        var definitions = Pair(Model.NovelVersion);
        var party = TowerPartySelection.Choice("fixture", definitions[0].References[0].Scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds));
        var nominees = new[] { TowerTeamCoverageSearch.Version, TowerDiscoveryRefinementSearch.Version }
            .SelectMany(policy => new[] { 1, 2 }.Select(rank => new TowerDiscoveryComparisonNomination(policy, rank, party))).ToArray();
        Assert.Throws<InvalidDataException>(() => Model.Nominations(definitions, nominees));
    }
}
