using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerStorageTests
{
    [Fact]
    public void Boundaries_count_active_pending_files_and_metadata_replacements_exactly()
    {
        using var temp = new Temp();
        File.WriteAllText(temp.P("fixed"), "123");
        var storage = new TowerStorageAccountant(temp.Path, 10, ["result", "result.pending"]);
        storage.BeginDirectory(temp.P("batch")); Directory.CreateDirectory(temp.P("batch"));
        File.WriteAllText(temp.P("batch/.pending"), "1234");
        File.WriteAllText(temp.P("result.pending"), "123");
        Assert.Equal(10, storage.Check());
        File.AppendAllText(temp.P("batch/.pending"), "x");
        Assert.Throws<InvalidDataException>(() => storage.Check());
        File.WriteAllText(temp.P("batch/.pending"), "1234");
        File.Move(temp.P("result.pending"), temp.P("result"));
        storage.SealDirectory(); storage.Audit(); Assert.Equal(10, storage.Check());
        File.WriteAllText(temp.P("result"), "1"); Assert.Equal(8, storage.Check()); storage.Audit();
    }

    [Theory]
    [InlineData("changed")]
    [InlineData("deleted")]
    [InlineData("extra-file")]
    [InlineData("extra-directory")]
    [InlineData("oversize")]
    public void Closed_subtree_external_changes_fail_the_mandatory_audit(string defect)
    {
        using var temp = new Temp(); Directory.CreateDirectory(temp.P("sealed"));
        File.WriteAllText(temp.P("sealed/file"), "123");
        var storage = new TowerStorageAccountant(temp.Path, 10, []);
        switch (defect)
        {
            case "changed": File.WriteAllText(temp.P("sealed/file"), "456"); break;
            case "deleted": File.Delete(temp.P("sealed/file")); break;
            case "extra-file": File.WriteAllText(temp.P("sealed/.hidden.pending"), "x"); break;
            case "extra-directory": Directory.CreateDirectory(temp.P("sealed/unknown")); break;
            default: File.WriteAllText(temp.P("sealed/file"), new string('x', 11)); break;
        }
        Assert.Equal(3, storage.Check()); // The versioned contract explicitly defers closed-subtree detection.
        Assert.Throws<InvalidDataException>(() => storage.Audit());
    }

    [Fact]
    public void Failed_writer_cannot_finalize_and_reopening_reconstructs_actual_bytes()
    {
        using var temp = new Temp(); var storage = new TowerStorageAccountant(temp.Path, 10, []);
        storage.BeginDirectory(temp.P("batch")); Directory.CreateDirectory(temp.P("batch"));
        File.WriteAllText(temp.P("batch/pending"), "123");
        Assert.Equal(3, storage.Check()); Assert.Throws<InvalidDataException>(() => storage.Audit());
        var reopened = new TowerStorageAccountant(temp.Path, 10, []);
        Assert.Equal(3, reopened.Check()); reopened.Audit();
        // Reconstructing bytes is not permission to resume a historical or owned execute-once campaign.
        Assert.Throws<InvalidDataException>(() => reopened.BeginDirectory(temp.P("batch")));
    }

    [Fact]
    public void Parent_includes_live_child_journal_once_and_seals_without_per_batch_parent_scans()
    {
        using var temp = new Temp(); File.WriteAllText(temp.P("attempts"), "S");
        var parent = new TowerStorageAccountant(temp.Path, 8, ["attempts"]);
        parent.BeginDirectory(temp.P("child")); Directory.CreateDirectory(temp.P("child"));
        var child = new TowerStorageAccountant(temp.P("child"), 10, ["journal"]); parent.Attach(child);
        child.BeginDirectory(temp.P("child/batch")); Directory.CreateDirectory(temp.P("child/batch"));
        File.WriteAllText(temp.P("child/batch/records"), "1234");
        File.WriteAllText(temp.P("child/journal"), "SS");
        Assert.Equal(7, parent.Check());
        File.AppendAllText(temp.P("attempts"), "CC");
        Assert.Throws<InvalidDataException>(() => parent.Check());
        File.WriteAllText(temp.P("attempts"), "SC");
        child.SealDirectory(); parent.SealDirectory();
        Assert.Equal(8, parent.Check()); parent.Audit();
        File.WriteAllText(temp.P("child/journal"), "CC");
        Assert.Throws<InvalidDataException>(() => parent.Audit());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Actual_writer_lease_is_counted_once_inside_parent_and_batch_ownership(bool nested)
    {
        using var temp = new Temp();
        if (nested) Directory.CreateDirectory(temp.P("batches"));
        var storage = new TowerStorageAccountant(temp.Path, 5, []);
        var path = temp.P(nested ? "batches/candidate" : "campaign"); storage.BeginDirectory(path);
        using (var lease = TowerCompactBundle.AcquireWriter(path))
        {
            Directory.CreateDirectory(path); File.WriteAllText(System.IO.Path.Combine(path, "records"), "123");
            lease.SetLength(2); lease.Flush(true);
            Assert.Equal(5, storage.Check());
            lease.SetLength(3); lease.Flush(true);
            Assert.Throws<InvalidDataException>(() => storage.Check());
            lease.SetLength(0); lease.Flush(true);
        }
        Assert.False(File.Exists(path + ".writer.lock"));
        storage.SealDirectory(); Assert.Equal(3, storage.Check()); storage.Audit();
    }

    [Fact]
    public void New_checks_have_constant_operation_counts_as_closed_batch_count_grows()
    {
        using var temp = new Temp(); Directory.CreateDirectory(temp.P("batches"));
        var storage = new TowerStorageAccountant(temp.Path, 1_000_000, []);
        long CountCheck()
        {
            var trace = new TowerPerformanceTrace();
            using (trace.Activate()) storage.Check();
            return trace.Snapshot().Where(t => t.Path.EndsWith("visited", StringComparison.Ordinal)).Sum(t => t.Calls);
        }
        var before = CountCheck();
        for (var i = 0; i < 64; i++)
        {
            var path = temp.P("batches/" + i); storage.BeginDirectory(path); Directory.CreateDirectory(path);
            File.WriteAllText(System.IO.Path.Combine(path, "record"), "x"); storage.SealDirectory();
        }
        Assert.Equal(before, CountCheck()); Assert.Equal(64, storage.Check()); storage.Audit();
    }

    [Fact]
    public void Cancellation_unknown_metadata_and_path_escape_are_rejected()
    {
        using var temp = new Temp(); var storage = new TowerStorageAccountant(temp.Path, 10, []);
        using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => storage.Check(stop.Token));
        Assert.ThrowsAny<OperationCanceledException>(() => storage.Audit(stop.Token));
        Assert.Throws<InvalidDataException>(() => storage.BeginDirectory(temp.P("../escape")));
        File.WriteAllText(temp.P(".unexpected.pending"), "x");
        Assert.Throws<InvalidDataException>(() => storage.Check());
    }

    [Fact]
    public void Manifest_bytes_are_included_before_completion_publication()
    {
        using var temp = new Temp(); var storage = new TowerStorageAccountant(temp.Path, 5, ["manifest.pending"]);
        File.WriteAllText(temp.P("manifest.pending"), "123456");
        Assert.Throws<InvalidDataException>(() => storage.Audit());
        Assert.False(File.Exists(temp.P("manifest")));
    }

    [Fact]
    public void Legacy_options_keep_their_serialized_shape()
    {
        Assert.DoesNotContain("storageAccounting", System.Text.Json.JsonSerializer.Serialize(new TowerBulkOptions(), HarnessJson.Options));
        Assert.Contains("owned-storage-v1", System.Text.Json.JsonSerializer.Serialize(new TowerBulkOptions(StorageAccounting: TowerStorageAccountant.Mode), HarnessJson.Options));
    }

    [Theory]
    [InlineData("owned-storage-v1", true)]
    [InlineData("unknown-storage", false)]
    public void Owned_resume_and_unknown_modes_fail_before_creating_any_campaign(string mode, bool resume)
    {
        using var temp = new Temp(); var output = temp.P("campaign");
        Assert.Throws<InvalidDataException>(() => TowerBulkCampaign.Open("unused", output, TowerCompactDiscovery.Kind,
            new { }, new Dictionary<string, string>(), "", "", 1, 1, new TowerBulkOptions(RetryReserve: 0, StorageAccounting: mode),
            resume, false, CancellationToken.None, null));
        Assert.False(Directory.Exists(output));
    }

    [Fact]
    public void Directory_links_fail_initialization_active_checks_and_closed_audits()
    {
        using var temp = new Temp(); Directory.CreateDirectory(temp.P("target"));
        var storage = new TowerStorageAccountant(temp.Path, 100, []);
        var link = temp.P("linked");
        void CreateLink()
        {
            if (OperatingSystem.IsWindows())
            {
                using var command = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe",
                    "/c mklink /J \"" + link + "\" \"" + temp.P("target") + "\"")
                    { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!;
                command.WaitForExit(); Assert.Equal(0, command.ExitCode);
            }
            else Directory.CreateSymbolicLink(link, temp.P("target"));
        }
        CreateLink();
        try
        {
            Assert.Throws<InvalidDataException>(() => new TowerStorageAccountant(temp.Path, 100, []));
            Assert.Throws<InvalidDataException>(() => storage.Audit());
        }
        finally { Directory.Delete(link); }
        storage.BeginDirectory(link);
        Directory.CreateDirectory(link);
        // A failed/pending writer cannot bypass ancestor validation at its next check.
        Directory.Delete(link);
        CreateLink();
        try { Assert.Throws<InvalidDataException>(() => storage.Check()); }
        finally { Directory.Delete(link); }
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-storage-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public string P(string name) => System.IO.Path.Combine(Path, name);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
