using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessPendingStorageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-pending-" + Guid.NewGuid().ToString("N"));
    public BalanceHarnessPendingStorageTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);
    private string P(string name) => Path.Combine(root, name);
    private PendingFileBudget File(string name = "result", long bytes = 16, int creates = 1)
        => new(P(name + ".pending"), P(name), bytes, creates);
    private TowerPendingStorage Scope(long file = 16, long live = 16, long total = 16, int creates = 1)
        => new(new([File(bytes: file, creates: creates)], live, total));
    private Stream Open(string name = "result") => TowerWorkAccounting.OpenWrite(P(name + ".pending"),
        () => new FileStream(P(name + ".pending"), FileMode.CreateNew, FileAccess.Write), scratch: true);
    private static JsonElement Value(TowerPendingStorage scope) => JsonSerializer.SerializeToElement(scope.Snapshot(), HarnessJson.Options);
    private static long Count(TowerPendingStorage scope, string name) => Value(scope).GetProperty(name).GetInt64();
    private void Publish(string name = "result", bool overwrite = false)
        => TowerWorkAccounting.MoveFile(P(name + ".pending"), P(name), overwrite);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Exact_cap_publication_preserves_bytes_and_optional_accounting(bool accounting)
    {
        var scope = Scope(); var work = new TowerWorkAccounting();
        using var active = accounting ? work.Activate() : null;
        await scope.RunAsync(() => {
            using (var stream = Open()) { stream.Write(new byte[16]); TowerWorkAccounting.FlushToDisk(stream); }
            Publish(); return Task.FromResult(7);
        });
        Assert.Equal(new byte[16], System.IO.File.ReadAllBytes(P("result")));
        Assert.Equal("Complete", Value(scope).GetProperty("outcome").GetString());
        Assert.Equal(16, Count(scope, "acceptedWriteBytes")); Assert.Equal(0, Count(scope, "trackedLiveBytes"));
        Assert.Equal(16, Count(scope, "peakTrackedLiveBytes")); Assert.Equal(16, Count(scope, "publishedBytes"));
        Assert.False(Value(scope).GetProperty("usableForAdmission").GetBoolean());
        if (accounting) Assert.Equal(16, work.Snapshot()["applicationWriteBytes.other"]);
    }

    [Theory]
    [InlineData(0, 16, 16)]
    [InlineData(16, 0, 16)]
    [InlineData(16, 16, 0)]
    public async Task Each_byte_cap_rejects_before_write(long file, long live, long total)
    {
        var scope = Scope(file, live, total);
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            using var stream = Open(); stream.WriteByte(1); return Task.FromResult(0);
        }));
        Assert.Equal(0, new FileInfo(P("result.pending")).Length); Assert.Equal(0, Count(scope, "acceptedWriteBytes"));
    }

    [Fact]
    public async Task Atomic_replacements_charge_every_pending_lifetime()
    {
        var scope = Scope(total: 12, creates: 2);
        System.IO.File.WriteAllBytes(P("result"), new byte[100]);
        var storage = new TowerCompleteReservation.Storage(root, 100000);
        await scope.RunAsync(() => {
            storage.PutBytes("result", new byte[5], true); storage.PutBytes("result", new byte[7], true);
            return Task.FromResult(0);
        });
        Assert.Equal(12, Count(scope, "acceptedWriteBytes")); Assert.Equal(7, Count(scope, "peakTrackedLiveBytes"));
        Assert.Equal(7, new FileInfo(P("result")).Length);
    }

    [Fact]
    public async Task Deleted_bytes_do_not_restore_total_write_allowance()
    {
        var scope = Scope(total: 4, creates: 2);
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            using (var s = Open()) s.Write(new byte[4]);
            TowerWorkAccounting.DeleteFile(P("result.pending"));
            using (var s = Open()) s.WriteByte(1);
            return Task.FromResult(0);
        }));
        Assert.Equal(4, Count(scope, "deletedBytes")); Assert.Equal(4, Count(scope, "acceptedWriteBytes"));
    }

    [Fact]
    public async Task Repeated_empty_files_still_consume_create_allowance()
    {
        var scope = Scope(0, 0, 0);
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            using (Open()) { } Publish(); using (Open()) { } return Task.FromResult(0);
        }));
        Assert.False(System.IO.File.Exists(P("result.pending")));
    }

    [Fact]
    public async Task Concurrent_open_files_share_live_cap()
    {
        var scope = new TowerPendingStorage(new([File("a"), File("b")], 7, 100));
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            using var a = Open("a"); using var b = Open("b"); a.Write(new byte[4]); b.Write(new byte[4]); return Task.FromResult(0);
        }));
        Assert.Equal(4, Count(scope, "acceptedWriteBytes")); Assert.Equal(0, new FileInfo(P("b.pending")).Length);
    }

    [Fact]
    public async Task Undeclared_path_never_invokes_create_factory()
    {
        var scope = Scope(); var called = false;
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            TowerWorkAccounting.OpenWrite(P("other"), () => { called = true; return Stream.Null; }, true); return Task.FromResult(0);
        }));
        Assert.False(called); Assert.Empty(Directory.GetFiles(root));
    }

    [Fact]
    public async Task Existing_pending_file_is_untouched()
    {
        System.IO.File.WriteAllText(P("result.pending"), "existing"); var scope = Scope();
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => { using var s = Open(); return Task.FromResult(0); }));
        Assert.Equal("existing", System.IO.File.ReadAllText(P("result.pending")));
    }

    [Fact]
    public async Task Different_publication_destination_is_rejected()
    {
        var scope = Scope();
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            using (var s = Open()) s.WriteByte(1);
            TowerWorkAccounting.MoveFile(P("result.pending"), P("other"), false); return Task.FromResult(0);
        }));
        Assert.False(System.IO.File.Exists(P("other"))); Assert.True(System.IO.File.Exists(P("result.pending")));
    }

    [Fact]
    public async Task Move_failure_preserves_target_and_poisons_retry()
    {
        System.IO.File.WriteAllText(P("result"), "old"); var scope = Scope();
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            using (var s = Open()) s.WriteByte(1);
            Assert.Throws<IOException>(() => Publish());
            Assert.Throws<InvalidDataException>(() => Publish(overwrite: true));
            return Task.FromResult(0);
        }));
        Assert.Equal("old", System.IO.File.ReadAllText(P("result")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Closed_content_or_length_mutation_cannot_publish(bool sameLength)
    {
        var scope = Scope();
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            using (var s = Open()) s.WriteByte(1);
            System.IO.File.WriteAllBytes(P("result.pending"), sameLength ? [2] : [1, 2]); Publish(); return Task.FromResult(0);
        }));
    }

    [Fact]
    public async Task Open_pending_file_cannot_be_published()
    {
        var scope = Scope();
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            using var s = Open(); s.WriteByte(1); Publish(); return Task.FromResult(0);
        }));
    }

    [Fact]
    public async Task Unresolved_file_prevents_success_and_closes_writer()
    {
        var scope = Scope(); Stream? stream = null;
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            stream = Open(); stream.WriteByte(1); return Task.FromResult(0);
        }));
        Assert.False(stream!.CanWrite); Assert.True(System.IO.File.Exists(P("result.pending")));
    }

    [Fact]
    public async Task Scope_reuse_and_nesting_are_rejected()
    {
        var scope = Scope(); var nested = Scope();
        await scope.RunAsync(async () => {
            await Assert.ThrowsAsync<InvalidDataException>(() => nested.RunAsync(() => Task.FromResult(0)));
            return 0;
        });
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => Task.FromResult(0)));
    }

    [Fact]
    public async Task Original_body_failure_survives_scope_cleanup()
    {
        var error = new ApplicationException("literal body failure"); var scope = Scope();
        Assert.Same(error, await Assert.ThrowsAsync<ApplicationException>(() => scope.RunAsync<int>(() => {
            var s = Open(); s.WriteByte(1); throw error;
        })));
        using var unlocked = System.IO.File.Open(P("result.pending"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    [Fact]
    public async Task Failed_write_prefix_remains_unknown_and_original_error_survives()
    {
        var error = new IOException("hidden prefix"); var scope = Scope();
        Assert.Same(error, await Assert.ThrowsAsync<IOException>(() => scope.RunAsync(() => {
            using var s = TowerWorkAccounting.OpenWrite(P("result.pending"), () => new Partial(P("result.pending"), error), true);
            s.Write(new byte[8]); return Task.FromResult(0);
        })));
        Assert.Equal(1, new FileInfo(P("result.pending")).Length); Assert.Equal(0, Count(scope, "acceptedWriteBytes"));
        Assert.True(Value(scope).GetProperty("failedProgressMayBeUnknown").GetBoolean());
    }
    private sealed class Partial(string path, IOException error) : FileStream(path, FileMode.CreateNew, FileAccess.Write)
    {
        public override void Write(ReadOnlySpan<byte> bytes) { base.Write(bytes[..1]); throw error; }
    }

    [Fact]
    public async Task Async_appends_obey_cap_and_do_not_expose_seek_or_truncate()
    {
        var scope = Scope();
        await scope.RunAsync(async () => {
            using (var s = Open())
            {
                Assert.False(s.CanSeek); Assert.Throws<NotSupportedException>(() => s.SetLength(999));
                Assert.Throws<NotSupportedException>(() => s.Seek(0, SeekOrigin.Begin));
                await s.WriteAsync(new byte[16]);
            }
            Publish(); return 0;
        });
    }

    [Fact]
    public async Task Declaration_is_copied_before_caller_mutation()
    {
        var files = new[] { File(bytes: 1) }; var scope = new TowerPendingStorage(new(files, 100, 100));
        files[0] = File(bytes: 100);
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            using var s = Open(); s.Write(new byte[2]); return Task.FromResult(0);
        }));
    }

    [Fact]
    public void Invalid_and_aliased_declarations_fail_before_filesystem_changes()
    {
        foreach (var budget in new[] {
            new PendingStorageBudget([], 1, 1), new([File(bytes: -1)], 1, 1), new([File(creates: 0)], 1, 1),
            new([File()], -1, 1), new([File()], 1, -1), new([File(), File()], 1, 1),
            new([new("relative", P("out"), 1, 1)], 1, 1), new([new(P("a"), P("a"), 1, 1)], 1, 1),
            new([File(), new(P("x"), P("result.pending"), 1, 1)], 1, 1) })
            Assert.Throws<InvalidDataException>(() => new TowerPendingStorage(budget));
        Assert.Empty(Directory.GetFileSystemEntries(root));
    }

    [Theory]
    [InlineData("result.")]
    [InlineData("result ")]
    [InlineData("NUL.json")]
    [InlineData("COM1")]
    public void Ambiguous_windows_path_segments_are_rejected(string name)
    {
        Assert.Throws<InvalidDataException>(() => new TowerPendingStorage(new([new(P(name), P("target"), 1, 1)], 1, 1)));
        Assert.Empty(Directory.GetFileSystemEntries(root));
    }

    [Fact]
    public async Task Actual_json_and_gzip_writers_obey_pending_caps()
    {
        var scope = new TowerPendingStorage(new([File("json", 4096), File("gzip", 4096)], 8192, 8192));
        await scope.RunAsync(() => {
            HarnessJson.WriteNew(P("json.pending"), new { literal = true }, scratch: true);
            TowerCompactBundle.WriteGzip(P("gzip.pending"), new { literal = true }, scratch: true);
            Publish("json"); Publish("gzip"); return Task.FromResult(0);
        });
        Assert.Equal(new FileInfo(P("json")).Length + new FileInfo(P("gzip")).Length, Count(scope, "acceptedWriteBytes"));
    }

    [Fact]
    public async Task Directory_publication_releases_only_declared_verified_members()
    {
        var source = P("chunk.pending"); var target = P("chunk"); Directory.CreateDirectory(source);
        var budgets = new[] { "receipt.json", "records.json.gz" }.Select(n => new PendingFileBudget(Path.Combine(source, n), Path.Combine(target, n), 4096, 1)).ToArray();
        var scope = new TowerPendingStorage(new(budgets, 8192, 8192));
        await scope.RunAsync(() => {
            HarnessJson.WriteNew(budgets[0].Path, new { literal = true }, true);
            TowerCompactBundle.WriteGzip(budgets[1].Path, new { literal = true }, true);
            TowerWorkAccounting.PublishDirectory(source, target, Directory.Move); return Task.FromResult(0);
        });
        Assert.False(Directory.Exists(source)); Assert.Equal(0, Count(scope, "trackedLiveBytes"));
    }

    [Fact]
    public async Task Directory_publication_rejects_unregistered_extra_file()
    {
        Directory.CreateDirectory(P("dir")); var scope = new TowerPendingStorage(new([new(P("dir/a"), P("out/a"), 10, 1)], 10, 10));
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            using (var s = TowerWorkAccounting.OpenWrite(P("dir/a"), () => System.IO.File.Create(P("dir/a")), true)) s.WriteByte(1);
            System.IO.File.WriteAllText(P("dir/extra"), "x");
            TowerWorkAccounting.PublishDirectory(P("dir"), P("out"), Directory.Move); return Task.FromResult(0);
        }));
        Assert.False(Directory.Exists(P("out")));
    }

    [Fact]
    public async Task Export_literal_production_pending_boundary()
    {
        var export = Environment.GetEnvironmentVariable("LL_NATIVE_PENDING_EXPORT");
        var output = Path.GetFullPath(export ?? P("export")); Assert.False(Path.Exists(output)); Directory.CreateDirectory(output);
        var path = Path.Combine(output, "literal.json");
        var scope = new TowerPendingStorage(new([new(path + ".pending", path, 4096, 2)], 4096, 8192));
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Literal pending fixture entered combat.")).Activate();
        var work = new TowerWorkAccounting();
        using (work.Activate()) await scope.RunAsync(() => {
            var storage = new TowerCompleteReservation.Storage(output, 100000);
            storage.Put("literal.json", new { literal = 1 }); storage.Put("literal.json", new { literal = 2 }, true);
            return Task.FromResult(0);
        });
        HarnessJson.WriteNew(Path.Combine(output, "storage.json"), scope.Snapshot());
        HarnessJson.WriteNew(Path.Combine(output, "fixture.json"), new { actualCombat = 0, productionEntropyDraws = 0,
            scientificReservations = 0, nativeEncounterPreparations = 0, wholeProcessCoverage = false, usableForAdmission = false,
            producerPath = typeof(TowerWorkAccounting).Assembly.Location, producerSha256 = HarnessJson.FileHash(typeof(TowerWorkAccounting).Assembly.Location), counters = work.Snapshot() });
        HarnessJson.WriteNew(Path.Combine(output, "files.json"), Directory.GetFiles(output).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
    }
}
