using System.Buffers.Binary;
using BalanceHarness;
using S = BalanceHarness.TowerProposalStudy;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessRemainingReadTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-remaining-read-" + Guid.NewGuid().ToString("N"));
    public BalanceHarnessRemainingReadTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(65536)]
    public void Binary_read_preserves_bytes_and_counts_only_a_successful_return(int length)
    {
        var path = Path.Combine(root, "entropy.bin");
        var bytes = Enumerable.Range(0, length).Select(n => (byte)(n % 256)).ToArray();
        File.WriteAllBytes(path, bytes);
        Assert.Equal(File.ReadAllBytes(path), TowerWorkAccounting.ReadAllBytes(path));
        var work = new TowerWorkAccounting();
        using (work.Activate()) Assert.Equal(bytes, TowerWorkAccounting.ReadAllBytes(path));
        Assert.Equal(length, work.Snapshot()["applicationReadBytes.other"]);
        Assert.Equal(1, work.Snapshot()["byteReadOperationsAttempted"]);
        Assert.Equal(1, work.Snapshot()["byteReadOperationsCompleted"]);
        Assert.DoesNotContain("failedByteReadBytesUnknown", work.Snapshot().Keys);
        using var lease = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("missing-parent/missing")]
    [InlineData("directory")]
    [InlineData("locked")]
    public void Failed_read_keeps_native_exception_and_unknown_progress(string name)
    {
        var path = Path.Combine(root, name);
        if (name == "directory") Directory.CreateDirectory(path);
        if (name == "locked") File.WriteAllBytes(path, [1, 2, 3]);
        using var lease = name == "locked" ? File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None) : null;
        var expected = Record.Exception(() => File.ReadAllBytes(path));
        var work = new TowerWorkAccounting();
        using (work.Activate())
        {
            var actual = Record.Exception(() => TowerWorkAccounting.ReadAllBytes(path));
            Assert.NotNull(expected); Assert.NotNull(actual);
            Assert.Equal(expected.GetType(), actual.GetType()); Assert.Equal(expected.HResult, actual.HResult);
            Assert.Equal(expected.Message, actual.Message);
        }
        Assert.Equal(1, work.Snapshot()["byteReadOperationsFailed"]);
        Assert.Equal(1, work.Snapshot()["failedByteReadBytesUnknown"]);
        Assert.DoesNotContain("applicationReadBytes.other", work.Snapshot().Keys);
        Assert.DoesNotContain("byteReadOperationsCompleted", work.Snapshot().Keys);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reservation_audit_counts_literal_entropy_even_when_later_binding_fails(bool changeAllocation)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Read fixture cannot fight.")).Activate();
        var (context, plan, history) = BalanceHarnessProposalStudyTests.Fixture(placement: true);
        var inputs = new ProposalStudyInputs(plan, context, new(new(), 10), history, new(new Dictionary<string, string>(), history));
        var bytes = new byte[S.EntropyWords * 4];
        for (var i = 0; i < S.EntropyWords; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * 4, 4), 200000 + i);
        var allocation = S.Classify(bytes, history, plan.Version);
        File.WriteAllBytes(Path.Combine(root, "entropy.bin"), bytes);
        HarnessJson.WriteNew(Path.Combine(root, "allocation.json"), changeAllocation ? allocation with { Duplicates = 1 } : allocation);
        HarnessJson.WriteNew(Path.Combine(root, "entropy-intent.json"), new { version = plan.Version, words = S.EntropyWords,
            assignedValues = plan.RequiredFreshValues, historicalHash = HarnessJson.Hash(history), retries = 0 });
        HarnessJson.WriteNew(Path.Combine(root, "seed-ledger.json"), new { reservationState = "Complete", historical = history, reserved = allocation.Reserved });
        HarnessJson.WriteNew(Path.Combine(root, "history-input.json"), new { reservationState = "Complete", reserved = allocation.Reserved });
        var work = new TowerWorkAccounting();
        using (work.Activate())
        {
            if (changeAllocation) Assert.Throws<InvalidDataException>(() => S.VerifyReservation(root, inputs));
            else Assert.Equal(HarnessJson.Hash(allocation), HarnessJson.Hash(S.VerifyReservation(root, inputs)));
        }
        Assert.Equal(65536, work.Snapshot()["applicationReadBytes.other"]);
        Assert.Equal(1, work.Snapshot()["byteReadOperationsCompleted"]);
        Assert.DoesNotContain("byteReadOperationsFailed", work.Snapshot().Keys);
        Assert.True(work.Snapshot()["jsonParseCompleted"] > 0);
    }
}
