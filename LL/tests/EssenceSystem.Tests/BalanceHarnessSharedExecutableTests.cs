using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessRefinementLaunchFixture;
using Runtime = EssenceSystem.Tests.BalanceHarnessRefinementComparisonFixture;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessSharedExecutableTests
{
    private static string Root()
    {
        var root = Path.Combine(Environment.GetEnvironmentVariable("TOWER_REFINEMENT_FIXTURE_ROOT") ?? Path.GetTempPath(), "shared-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); return root;
    }
    private static string Shared()
    {
        var root = Root(); new Runtime().CreateSharedExecutable(root, 1048576, default); return root;
    }

    [Fact] public async Task Compact_json_preserves_semantics_and_restores_nested_async_contexts()
    {
        var value = new { text = "a\nb Ω", reserved = new[] { int.MinValue, 0, int.MaxValue } };
        var pretty = JsonSerializer.Serialize(value, HarnessJson.Options); var hash = HarnessJson.Hash(value);
        Assert.Contains('\n', pretty);
        await Task.WhenAll(Task.Run(async () => {
            using (HarnessJson.UseCompactOutput()) {
                await Task.Yield(); Assert.DoesNotContain('\n', JsonSerializer.Serialize(value, HarnessJson.Options));
                using (HarnessJson.UseCompactOutput()) Assert.Equal(hash, HarnessJson.Hash(value));
                Assert.False(HarnessJson.Options.WriteIndented);
            }
            Assert.True(HarnessJson.Options.WriteIndented);
        }), Task.Run(() => Assert.Equal(pretty, JsonSerializer.Serialize(value, HarnessJson.Options))));
        Assert.Equal(pretty, JsonSerializer.Serialize(value, HarnessJson.Options));
    }

    [Fact] public void Unknown_profile_is_rejected_and_legacy_contract_omits_new_fields()
    {
        var f = new F(); Assert.DoesNotContain("archiveProfile", JsonSerializer.Serialize(f.Request, HarnessJson.Options));
        Assert.DoesNotContain("sharedExecutablePath", JsonSerializer.Serialize(new TowerBulkOptions(), HarnessJson.Options));
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Validate(f.Request with { ArchiveProfile = "unknown" }));
        Assert.False(Directory.Exists(f.Request.StudyRoot));
    }

    [Fact] public void Bounded_copy_stops_before_exceeding_bytes_and_preserves_partial_evidence()
    {
        var root = Root(); var source = Path.Combine(root, "source"); var target = Path.Combine(root, "target");
        File.WriteAllBytes(source, new byte[65537]);
        Assert.Throws<InvalidDataException>(() => TowerBossStudy.CopyBounded(source, target, 65536, default));
        Assert.Equal(65536, new FileInfo(target).Length);
        Assert.Throws<IOException>(() => TowerBossStudy.CopyBounded(source, target, 70000, default));
        using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.Throws<OperationCanceledException>(() => TowerBossStudy.CopyBounded(source, Path.Combine(root, "cancelled"), 70000, stop.Token));
    }

    [Fact] public void Shared_creation_cap_and_retry_cannot_publish_a_second_bundle()
    {
        var root = Root(); Assert.Throws<InvalidDataException>(() => new Runtime().CreateSharedExecutable(root, 65535, default));
        Assert.False(File.Exists(Path.Combine(root, TowerSharedExecutable.Manifest)));
        new Runtime().CreateSharedExecutable(root, 1048576, default);
        Assert.Throws<InvalidDataException>(() => new Runtime().CreateSharedExecutable(root, 1048576, default));
    }

    [Fact] public void Shared_inventory_rejects_modified_missing_extra_and_wrong_identity()
    {
        var root = Shared(); var file = Path.Combine(root, "executable/fixture.dll");
        Assert.Throws<InvalidDataException>(() => TowerSharedExecutable.Verify(root, Runtime.SharedIdentity with { Runtime = "changed" }, default));
        File.WriteAllBytes(file, [3, 2, 1]); Assert.Throws<InvalidDataException>(() => new Runtime().VerifySharedExecutable(root, default));
        File.WriteAllBytes(file, [1, 2, 3]); var extra = Path.Combine(root, "executable/.hidden"); File.WriteAllText(extra, "x");
        File.SetAttributes(extra, FileAttributes.Hidden); Assert.Throws<InvalidDataException>(() => new Runtime().VerifySharedExecutable(root, default));
        File.SetAttributes(extra, FileAttributes.Normal); File.Delete(extra); File.Delete(file);
        Assert.Throws<InvalidDataException>(() => new Runtime().VerifySharedExecutable(root, default));
    }

    [Fact] public void Stage_reference_rejects_escape_and_changed_manifest()
    {
        var root = Shared(); var stage = Path.Combine(root, "stage"); Directory.CreateDirectory(stage);
        TowerSharedExecutable.WriteReference(stage, Runtime.SharedIdentity, default);
        var path = Path.Combine(stage, TowerSharedExecutable.Reference);
        var reference = HarnessJson.Read<TowerSharedExecutableReference>(path);
        File.WriteAllText(path, JsonSerializer.Serialize(reference with { Path = "../../executable" }, HarnessJson.Options));
        Assert.Throws<InvalidDataException>(() => TowerSharedExecutable.VerifyReference(stage, Runtime.SharedIdentity, default));
        File.WriteAllText(path, JsonSerializer.Serialize(reference, HarnessJson.Options));
        File.AppendAllText(Path.Combine(root, TowerSharedExecutable.Manifest), " ");
        Assert.Throws<InvalidDataException>(() => TowerSharedExecutable.VerifyReference(stage, Runtime.SharedIdentity, default));
    }

    [Fact] public void Complete_shared_archive_is_relocatable_and_does_not_reference_producing_directory()
    {
        var root = Shared(); var stage = Path.Combine(root, "stage"); Directory.CreateDirectory(stage);
        TowerSharedExecutable.WriteReference(stage, Runtime.SharedIdentity, default);
        var moved = Root();
        foreach (var file in TowerBulkCampaign.Paths(root)) {
            var target = Path.Combine(moved, Path.GetRelativePath(root, file)); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file, target);
        }
        File.WriteAllBytes(Path.Combine(root, "executable/fixture.dll"), [0]);
        TowerSharedExecutable.VerifyReference(Path.Combine(moved, "stage"), Runtime.SharedIdentity, default);
    }

    [Fact] public async Task Opt_in_launch_preserves_schedules_nominations_quality_and_attempts()
    {
        var old = new F(); old.Bind(); var reference = await old.Run();
        var next = new F(TowerSharedExecutable.Profile); next.Bind(); var candidate = await next.Run();
        Assert.Equal(HarnessJson.Hash(reference), HarnessJson.Hash(candidate));
        foreach (var name in new[] { "seeds.json", "run/nominations.json", "run/finalists.json", "run/family.json" })
            Assert.Equal(HarnessJson.Hash(HarnessJson.Read<JsonElement>(old.P(name))), HarnessJson.Hash(HarnessJson.Read<JsonElement>(next.P(name))));
        Assert.Equal(File.ReadAllBytes(old.P("run/attempts.bin")), File.ReadAllBytes(next.P("run/attempts.bin")));
        Assert.DoesNotContain('\n', File.ReadAllText(next.P("seeds.json")));
        Assert.Single(Directory.GetDirectories(next.P("run"), "executable", SearchOption.AllDirectories));
        Assert.Equal(4, Directory.GetFiles(next.P("run"), TowerSharedExecutable.Reference, SearchOption.AllDirectories).Length);
        TowerBulkCampaign.VerifyFiles(next.Request.StudyRoot, "launch-files.json", true, default);
        File.WriteAllBytes(next.P("run/executable/fixture.dll"), [0]);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerRefinementComparisonRun.Reconstruct(next.P("run"), new Runtime()));
    }

    [Fact] public void Compact_reservation_keeps_returned_labels_pending_after_cancellation()
    {
        var f = new F(TowerSharedExecutable.Profile); using var stop = new CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() => f.Bind(stop.Token, (_, _) => { stop.Cancel(); return 17; }));
        Assert.Equal(17, HarnessJson.Read<JsonElement>(f.P("history-input.json")).GetProperty("reserved")[0].GetInt32());
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(f.P("history-input.json"))));
        Assert.Equal(2, File.ReadAllLines(f.P("allocation-journal.jsonl")).Length);
        Assert.False(File.Exists(f.P(TowerRefinementComparisonLaunch.BindingFiles)));
        Assert.True(HarnessJson.Options.WriteIndented);
    }

    [Fact] public async Task Authorized_archive_profile_cannot_be_silently_downgraded()
    {
        var f = new F(TowerSharedExecutable.Profile); f.Bind();
        await Assert.ThrowsAsync<InvalidDataException>(() => f.Run(execute: (p, d, s, b, ct) =>
            TowerRefinementComparisonRun.Execute(p, d, s, b, new Runtime(), ct)));
        Assert.True(File.Exists(f.P("launch-failure.json")));
        await Assert.ThrowsAsync<InvalidDataException>(() => f.Run());
    }
}
