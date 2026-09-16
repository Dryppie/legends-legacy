using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessRefinementLaunchFixture;
using Runtime = EssenceSystem.Tests.BalanceHarnessRefinementComparisonFixture;
using Model = BalanceHarness.TowerRefinementComparisonModel;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessFreshFirstVersionTests
{
    static F Local() => new(TowerSharedExecutable.Profile, Model.FreshFirstVersion);
    static TowerBossDiscoveryDefinition[] Pair() {
        var source = Runtime.Definitions()[0]; var s = source.Stages.Schedules.Values.Single();
        var labels = new TowerRefinementComparisonSeeds(source.ExcludedCombatSeeds.ToArray(), source.Generation.Seeds.ToArray(), s.Discovery.ToArray(), s.Selection.ToArray(), s.Confirmation.ToArray());
        return Model.Policies.Select(p => Model.Definition(source, labels, p, Model.FreshFirstVersion)).ToArray();
    }
    [Fact] public void Fresh_first_pair_changes_only_version_and_policy_with_identical_schedules() {
        var old = Runtime.Definitions(); var next = Pair(); Model.ValidatePair(next);
        Assert.Equal(TowerDiscoveryRefinementSearch.FreshFirstVersion, next[1].Generation.PolicyVersion);
        for (var i = 0; i < 2; i++) {
            Assert.Equal(Model.FreshFirstVersion, next[i].Id);
            Assert.Equal(HarnessJson.Hash(old[i]), HarnessJson.Hash(next[i] with { Id = old[i].Id, Generation = next[i].Generation with { PolicyVersion = old[i].Generation.PolicyVersion } }));
        }
    }
    [Fact] public void Fresh_first_preflight_requires_the_matching_verified_driver_receipt() {
        foreach (var policy in new[] { TowerDiscoveryRefinementSearch.Version, TowerDiscoveryRefinementSearch.LocalVersion, TowerDiscoveryRefinementSearch.FreshFirstVersion }) {
            var receipt = JsonSerializer.SerializeToElement(new { status = "VerifiedRefinementComparisonDriver", reservations = 1, comparisonVersion = Model.FreshFirstVersion, refinementPolicy = policy });
            if (policy == TowerDiscoveryRefinementSearch.FreshFirstVersion) TowerRefinementComparisonPreflight.VerifyDriverReceipt(receipt, 1, Model.FreshFirstVersion);
            else Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonPreflight.VerifyDriverReceipt(receipt, 1, Model.FreshFirstVersion));
        }
        var missing = JsonSerializer.SerializeToElement(new { status = "VerifiedRefinementComparisonDriver", reservations = 1 });
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonPreflight.VerifyDriverReceipt(missing, 1, Model.FreshFirstVersion));
    }
    [Fact] public void Changed_version_cannot_reuse_authorization_or_allocate_a_label() {
        var old = new F(TowerSharedExecutable.Profile, Model.LocalVersion); var inspected = false; var allocated = false;
        var changed = old.Request with { Preflight = old.Request.Preflight with { ComparisonVersion = Model.FreshFirstVersion } };
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.BindCore(changed, old.Permit,
            _ => { inspected = true; return old.Inputs; }, (_, _) => { }, default, (_, _) => { allocated = true; return 17; }));
        Assert.False(inspected); Assert.False(allocated); Assert.False(Directory.Exists(changed.StudyRoot));
    }
    [Fact] public async Task Fresh_first_binding_and_reconstruction_preserve_charges_and_archive_integrity() {
        var f = Local(); f.Bind(); Assert.Equal(HarnessJson.Hash(Pair()), HarnessJson.Hash(HarnessJson.Read<TowerBossDiscoveryDefinition[]>(f.P("definitions.json"))));
        var quality = await f.Run(); var verified = await TowerRefinementComparisonRun.Reconstruct(f.P("run"), new Runtime());
        Assert.Equal(HarnessJson.Hash(quality), HarnessJson.Hash(verified));
        Assert.Equal(TowerDiscoveryComparisonGate.FreshFirstVersion, HarnessJson.Read<TowerDiscoveryComparisonDecision>(f.P("run/discovery-gate.json")).Version);
        TowerRescreenAttempts.Verify(f.P("run/attempts.bin"), 240); TowerBulkCampaign.VerifyFiles(f.Request.StudyRoot, "launch-files.json", true, default);
        await Assert.ThrowsAsync<InvalidDataException>(() => f.Run());
    }
    [Fact] public async Task Incomplete_fresh_first_discovery_preserves_charges_and_stops_selection() {
        var f = Local(); f.Bind(); var runtime = new Runtime("partial-candidate");
        await Assert.ThrowsAsync<InvalidDataException>(() => f.Run(execute: (p, d, s, b, ct) => TowerRefinementComparisonRun.Execute(p, d, s, b, runtime, ct, f.Request.ArchiveProfile)));
        var gate = HarnessJson.Read<TowerDiscoveryComparisonDecision>(f.P("run/discovery-gate.json"));
        Assert.Equal(TowerDiscoveryComparisonGate.FreshFirstVersion, gate.Version); Assert.Equal("StoppedDiscovery", gate.Status);
        TowerRescreenAttempts.Verify(f.P("run/attempts.bin"), 120); Assert.False(File.Exists(f.P("run/nominations.json"))); Assert.DoesNotContain("selection", runtime.Calls);
    }
    [Fact] public void Wrong_candidate_versions_and_nominations_are_rejected() {
        var definitions = Pair(); var wrong = definitions.ToArray(); wrong[1] = wrong[1] with { Generation = wrong[1].Generation with { PolicyVersion = TowerDiscoveryRefinementSearch.LocalVersion } };
        Assert.Throws<InvalidDataException>(() => Model.ValidatePair(wrong));
        var party = TowerPartySelection.Choice("fixture", definitions[0].References[0].Scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds));
        var nominations = new[] { TowerTeamCoverageSearch.Version, TowerDiscoveryRefinementSearch.LocalVersion }.SelectMany(policy => new[] { 1, 2 }
            .Select(rank => new TowerDiscoveryComparisonNomination(policy, rank, party))).ToArray();
        Assert.Throws<InvalidDataException>(() => Model.Nominations(definitions, nominations));
    }
}
