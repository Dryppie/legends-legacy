using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessLeaseAccountingTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-lease-" + Guid.NewGuid().ToString("N"));
    public BalanceHarnessLeaseAccountingTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Real_lease_preserves_exclusion_and_delete_on_close(bool active)
    {
        var path = Path.Combine(root, "result"); var work = new TowerWorkAccounting();
        using (active ? work.Activate() : null)
        {
            using (var lease = TowerCompactBundle.AcquireWriter(path))
            {
                if (!active) Assert.IsType<FileStream>(lease);
                Assert.True(File.Exists(path + ".writer.lock"));
                Assert.Equal(0, new FileInfo(path + ".writer.lock").Length);
                Assert.Throws<IOException>(() => TowerCompactBundle.AcquireWriter(path));
            }
        }
        Assert.False(File.Exists(path + ".writer.lock"));
        if (!active) { Assert.Empty(work.Snapshot()); return; }
        Assert.Equal(2, work.Snapshot()["writerLeaseAcquireAttempted"]);
        Assert.Equal(1, work.Snapshot()["writerLeaseAcquireFailed"]);
        Assert.Equal(1, work.Snapshot()["writerLeaseAcquireCompleted"]);
        Assert.Equal(1, work.Snapshot()["writerLeaseReleaseCompleted"]);
    }

    [Fact]
    public void Expected_held_probe_is_complete_even_though_acquisition_was_rejected()
    {
        var path = Path.Combine(root, "held"); var work = new TowerWorkAccounting();
        using var held = TowerCompactBundle.AcquireWriter(path);
        using (work.Activate()) TowerWorkAccounting.RequireWriterLeaseHeld(path);
        Assert.Equal(1, work.Snapshot()["writerLeaseProbeHeld"]);
        Assert.Equal(1, work.Snapshot()["writerLeaseProbeCompleted"]);
        Assert.Equal(1, work.Snapshot()["writerLeaseAcquireFailed"]);
        Assert.DoesNotContain("writerLeaseProbeFailed", work.Snapshot().Keys);
        Assert.DoesNotContain("writerLeaseReleaseAttempted", work.Snapshot().Keys);
    }

    [Fact]
    public void Missing_owner_probe_releases_its_handle_and_fails()
    {
        var path = Path.Combine(root, "missing"); var work = new TowerWorkAccounting();
        using (work.Activate())
            Assert.Equal("Missing enclosing registry/output ownership.",
                Assert.Throws<InvalidDataException>(() => TowerWorkAccounting.RequireWriterLeaseHeld(path)).Message);
        Assert.False(File.Exists(path + ".writer.lock"));
        Assert.Equal(1, work.Snapshot()["writerLeaseProbeMissing"]);
        Assert.Equal(1, work.Snapshot()["writerLeaseProbeFailed"]);
        Assert.Equal(1, work.Snapshot()["writerLeaseReleaseCompleted"]);
    }

    [Fact]
    public void Nonsharing_probe_error_is_not_accepted_as_ownership()
    {
        var path = Path.Combine(root, "directory"); Directory.CreateDirectory(path + ".writer.lock");
        var work = new TowerWorkAccounting();
        using (work.Activate()) Assert.Throws<UnauthorizedAccessException>(() => TowerWorkAccounting.RequireWriterLeaseHeld(path));
        Assert.Equal(1, work.Snapshot()["writerLeaseProbeFailed"]);
        Assert.Equal(1, work.Snapshot()["writerLeaseAcquireFailed"]);
        Assert.DoesNotContain("writerLeaseProbeHeld", work.Snapshot().Keys);
    }

    [Theory]
    [InlineData("dispose")]
    [InlineData("close")]
    [InlineData("async")]
    public async Task Lease_retains_its_stream_contract_collector_and_disposes_once(string method)
    {
        var first = new TowerWorkAccounting(); var second = new TowerWorkAccounting(); FileStream lease;
        using (first.Activate()) lease = TowerCompactBundle.AcquireWriter(Path.Combine(root, "scope"));
        using (second.Activate())
        {
            lease.SetLength(4); lease.Flush(); Assert.Equal(4, lease.Length);
            if (method == "async") await lease.DisposeAsync();
            else if (method == "close") lease.Close();
            else lease.Dispose();
            lease.Dispose();
        }
        Assert.Equal(1, first.Snapshot()["writerLeaseReleaseAttempted"]);
        Assert.Equal(1, first.Snapshot()["writerLeaseReleaseCompleted"]);
        Assert.Empty(second.Snapshot());
    }

    [Fact]
    public void Failed_dispose_retains_original_exception_and_unknown_state_without_retry()
    {
        var failure = new IOException("literal release failure"); var work = new TowerWorkAccounting();
        var lifetime = new TowerWorkAccounting.WriterLeaseLifetime(work); var calls = 0;
        void Release() { calls++; throw failure; }
        Assert.Same(failure, Assert.Throws<IOException>(() => lifetime.Release(Release)));
        lifetime.Release(Release); Assert.Equal(1, calls);
        Assert.Equal(1, work.Snapshot()["writerLeaseReleaseFailed"]);
        Assert.Equal(1, work.Snapshot()["writerLeaseReleaseStateUnknown"]);
        Assert.DoesNotContain("writerLeaseReleaseCompleted", work.Snapshot().Keys);
    }

    [Fact]
    public void Selected_metadata_counts_false_as_return_and_preserves_thrown_error()
    {
        var work = new TowerWorkAccounting(); var failure = new IOException("metadata");
        using (work.Activate())
        {
            Assert.False(TowerWorkAccounting.ObserveOperation("metadataFileExists", () => File.Exists(Path.Combine(root, "absent"))));
            Assert.Same(failure, Assert.Throws<IOException>(() => TowerWorkAccounting.ObserveOperation<bool>("metadataLinkTarget", () => throw failure)));
        }
        Assert.Equal(1, work.Snapshot()["metadataFileExistsCompleted"]);
        Assert.Equal(1, work.Snapshot()["metadataLinkTargetFailed"]);
        Assert.DoesNotContain("metadataLinkTargetCompleted", work.Snapshot().Keys);
        Assert.DoesNotContain(work.Snapshot().Keys, k => k.Contains("Bytes"));
    }

}
