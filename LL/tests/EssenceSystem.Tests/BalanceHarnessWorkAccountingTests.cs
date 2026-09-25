using System.Text;
using System.Text.Json;
using System.IO.Compression;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessWorkAccountingTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ll-work-counters-" + Guid.NewGuid().ToString("N"));
    private static readonly ProposalEvidenceLimits Limits = new(1048576, 1048576);
    public BalanceHarnessWorkAccountingTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);
    private ProposalEvidenceEntry Write(string text) => TowerProposalEvidenceCodec.WriteRawNew(root, "search.json",
        s => s.Write(Encoding.UTF8.GetBytes(text)), TowerProposalEvidenceCodec.Version, Limits, _ => { });
    private void Read(ProposalEvidenceEntry entry) => TowerProposalEvidenceCodec.Read<JsonElement>(root, entry, TowerProposalEvidenceCodec.Version, Limits);

    [Fact]
    public void Codec_counts_every_pass_and_exports_bound_cross_language_receipt()
    {
        var entry = Write(" {\"dragon\":\"🐉æ\",\"n\":-0.00e+10} \n");
        var ledger = new TowerWorkAccounting();
        ProposalEvidenceRead<JsonElement> read;
        using (ledger.Activate()) read = TowerProposalEvidenceCodec.Read<JsonElement>(root, entry, TowerProposalEvidenceCodec.Version, Limits);
        var counts = ledger.Snapshot();
        Assert.Equal(read.PhysicalBytesRead, counts["applicationReadBytes.payload"]);
        Assert.Equal(2 * entry.LogicalBytes, counts["decodedBytesProcessed"]);
        Assert.Equal(2, counts["decodePassesStarted"]);
        Assert.Equal(2, counts["decodePassesCompleted"]);
        Assert.Equal(entry.LogicalBytes, counts["jsonInputBytes"]);
        Assert.Equal(1, counts["jsonParseCompleted"]);
        if (Environment.GetEnvironmentVariable("LL_WORK_ACCOUNTING_EXPORT") is { Length: > 0 } export)
        {
            Assert.False(Directory.Exists(export)); Directory.CreateDirectory(export);
            File.Copy(Path.Combine(root, entry.PhysicalPath), Path.Combine(export, entry.PhysicalPath));
            HarnessJson.WriteNew(Path.Combine(export, "entry.json"), entry);
            HarnessJson.WriteNew(Path.Combine(export, "native-work.json"), ledger.Receipt("nativeAudit", new('a', 64), new('b', 64), true));
            HarnessJson.WriteNew(Path.Combine(export, "files.json"), Directory.GetFiles(export).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
        }
    }

    [Fact]
    public void Failed_authentication_retains_partial_decode_and_reads()
    {
        var entry = Write("{\"x\":123}") with { LogicalSha256 = new('0', 64) };
        var ledger = new TowerWorkAccounting();
        using (ledger.Activate()) Assert.Throws<InvalidDataException>(() => Read(entry));
        Assert.Equal(entry.LogicalBytes, ledger.Snapshot()["decodedBytesProcessed"]);
        Assert.True(ledger.Snapshot()["applicationReadBytes.payload"] > 0);
        Assert.Equal(1, ledger.Snapshot()["decodePassesStarted"]);
        Assert.False(ledger.Snapshot().ContainsKey("decodePassesCompleted"));
    }

    [Fact]
    public void Failed_parser_retains_first_verified_pass_and_second_partial_pass()
    {
        var entry = Write("{ invalid json }"); var ledger = new TowerWorkAccounting();
        using (ledger.Activate()) Assert.Throws<JsonException>(() => Read(entry));
        Assert.Equal(2, ledger.Snapshot()["decodePassesStarted"]);
        Assert.Equal(1, ledger.Snapshot()["decodePassesCompleted"]);
        Assert.Equal(1, ledger.Snapshot()["jsonParseAttempts"]);
        Assert.False(ledger.Snapshot().ContainsKey("jsonParseCompleted"));
        Assert.True(ledger.Snapshot()["jsonInputBytes"] > 0);
    }

    [Fact]
    public void Json_and_manifest_reads_are_counted_each_time_without_double_counting_parse()
    {
        var path = Path.Combine(root, "files.json"); File.WriteAllText(path, "{\"a\":1}");
        var ledger = new TowerWorkAccounting();
        using (ledger.Activate()) { HarnessJson.Read<JsonElement>(path); HarnessJson.FileHash(path); HarnessJson.FileHash(path); }
        Assert.Equal(3 * new FileInfo(path).Length, ledger.Snapshot()["applicationReadBytes.manifest"]);
        Assert.Equal(new FileInfo(path).Length, ledger.Snapshot()["jsonInputBytes"]);
    }

    public sealed record Contract(string Name);

    [Theory]
    [InlineData("utf8")]
    [InlineData("utf8-bom")]
    [InlineData("utf16")]
    public void Contract_manifest_counts_raw_bytes_and_preserves_BOM_decoding(string encoding)
    {
        var path = Path.Combine(root, "files.json"); const string json = "{\"name\":\"🐉æ\"}";
        var codec = encoding == "utf16" ? Encoding.Unicode : new UTF8Encoding(encoding == "utf8-bom");
        File.WriteAllText(path, json, codec);
        var expected = TowerContractJson.Read<Contract>(path);
        var ledger = new TowerWorkAccounting();
        using (ledger.Activate()) Assert.Equal(expected, TowerContractJson.Read<Contract>(path));
        Assert.Equal(new FileInfo(path).Length, ledger.Snapshot()["applicationReadBytes.manifest"]);
        Assert.Equal(Encoding.UTF8.GetByteCount(json), ledger.Snapshot()["jsonInputBytes"]);
        Assert.Equal(1, ledger.Snapshot()["jsonParseCompleted"]);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"name\":\"x\",\"extra\":1}")]
    [InlineData("{bad}")]
    public void Failed_strict_contract_parse_preserves_reads_and_attempt(string json)
    {
        var path = Path.Combine(root, "contract.json"); File.WriteAllText(path, json);
        Assert.Throws<JsonException>(() => TowerContractJson.Read<Contract>(path));
        var ledger = new TowerWorkAccounting();
        using (ledger.Activate()) Assert.Throws<JsonException>(() => TowerContractJson.Read<Contract>(path));
        Assert.Equal(new FileInfo(path).Length, ledger.Snapshot()["applicationReadBytes.json"]);
        Assert.Equal(Encoding.UTF8.GetByteCount(json), ledger.Snapshot()["jsonInputBytes"]);
        Assert.Equal(1, ledger.Snapshot()["jsonParseAttempts"]);
        Assert.False(ledger.Snapshot().ContainsKey("jsonParseCompleted"));
    }

    [Fact]
    public void Journal_parser_counts_consumed_lines_and_retains_buffered_read_on_failure()
    {
        var path = Path.Combine(root, "trials.jsonl");
        File.WriteAllText(path, "{\"name\":\"🐉\"}\r\n{bad}\n{\"unread\":1}\n", new UTF8Encoding(true));
        var lines = File.ReadAllLines(path); var ledger = new TowerWorkAccounting();
        using (ledger.Activate())
        {
            using var reader = TowerWorkAccounting.ReadLines(path).GetEnumerator();
            Assert.True(reader.MoveNext()); Assert.Equal(lines[0], reader.Current);
            TowerWorkAccounting.Parse<JsonElement>(reader.Current);
            Assert.True(reader.MoveNext());
            Assert.Throws<JsonException>(() => TowerWorkAccounting.Parse<JsonElement>(reader.Current));
        }
        Assert.Equal(new FileInfo(path).Length, ledger.Snapshot()["applicationReadBytes.journal"]);
        Assert.Equal(Encoding.UTF8.GetByteCount(lines[0] + lines[1]), ledger.Snapshot()["jsonInputBytes"]);
        Assert.Equal(2, ledger.Snapshot()["jsonParseAttempts"]);
        Assert.Equal(1, ledger.Snapshot()["jsonParseCompleted"]);
        using var exclusive = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Compressed_battle_reader_counts_payload_decoding_and_parser_even_on_failure(bool malformed)
    {
        Directory.CreateDirectory(Path.Combine(root, "battles"));
        var path = Path.Combine(root, "battles", "trial-000001.json.gz");
        var raw = Encoding.UTF8.GetBytes(malformed ? "{bad}" : "{\"battle\":null,\"succeeded\":true,\"guardianHealthRemainingPercent\":50,\"displayDurationSeconds\":1}");
        using (var file = File.Create(path))
        using (var gzip = new GZipStream(file, CompressionLevel.Fastest)) gzip.Write(raw);
        var ledger = new TowerWorkAccounting();
        using (ledger.Activate())
        {
            if (malformed) Assert.Throws<JsonException>(() => TowerLoadoutArchive.ReadBattle(root, "trial-000001", "gzip-json-v1"));
            else Assert.True(TowerLoadoutArchive.ReadBattle(root, "trial-000001", "gzip-json-v1").Succeeded);
        }
        Assert.Equal(new FileInfo(path).Length, ledger.Snapshot()["applicationReadBytes.payload"]);
        Assert.Equal(raw.Length, ledger.Snapshot()["decodedBytesProcessed"]);
        Assert.Equal(raw.Length, ledger.Snapshot()["jsonInputBytes"]);
        Assert.Equal(1, ledger.Snapshot()["decodePassesStarted"]);
        Assert.Equal(1, ledger.Snapshot()["jsonParseAttempts"]);
        Assert.Equal(malformed ? 0 : 1, ledger.Snapshot().GetValueOrDefault("jsonParseCompleted"));
        if (!malformed) Assert.Equal(1, ledger.Snapshot()["decodePassesCompleted"]);
    }

    [Fact]
    public async Task Decoder_requires_EOF_and_keeps_original_collector_across_scopes()
    {
        var outer = new TowerWorkAccounting(); var inner = new TowerWorkAccounting();
        using var source = new MemoryStream([1, 2, 3]); Stream decoded;
        using (outer.Activate()) decoded = TowerWorkAccounting.DecodeStream(source);
        using (inner.Activate())
        {
            Assert.Equal(0, await decoded.ReadAsync(Memory<byte>.Empty));
            Assert.False(outer.Snapshot().ContainsKey("decodePassesCompleted"));
            Assert.Equal(1, decoded.ReadByte());
            Assert.Equal(2, await decoded.ReadAsync(new byte[20]));
            Assert.False(outer.Snapshot().ContainsKey("decodePassesCompleted"));
            Assert.Equal(-1, decoded.ReadByte()); Assert.Equal(-1, decoded.ReadByte());
            decoded.Dispose();
        }
        Assert.True(source.CanRead);
        Assert.Empty(inner.Snapshot());
        Assert.Equal(3, outer.Snapshot()["decodedBytesProcessed"]);
        Assert.Equal(1, outer.Snapshot()["decodePassesCompleted"]);
        var partial = new TowerWorkAccounting();
        using (partial.Activate())
        using (var stream = TowerWorkAccounting.DecodeStream(new MemoryStream([1, 2]))) Assert.Equal(1, stream.ReadByte());
        Assert.False(partial.Snapshot().ContainsKey("decodePassesCompleted"));
    }

    [Fact]
    public async Task Stream_counts_async_seek_and_single_byte_reads()
    {
        var ledger = new TowerWorkAccounting();
        using var active = ledger.Activate();
        using var stream = TowerWorkAccounting.ReadStream(new MemoryStream([1, 2, 3, 4]), "attempts.jsonl");
        Assert.Equal(1, stream.ReadByte()); stream.Seek(0, SeekOrigin.Begin);
        Assert.Equal(4, await stream.ReadAsync(new byte[20]));
        Assert.Equal(-1, stream.ReadByte());
        Assert.Equal(5, ledger.Snapshot()["applicationReadBytes.journal"]);
    }

    [Fact]
    public void Nested_scopes_restore_even_after_repeated_dispose_and_exception()
    {
        var outer = new TowerWorkAccounting(); var inner = new TowerWorkAccounting();
        using (outer.Activate())
        {
            var scope = inner.Activate(); TowerWorkAccounting.Add("x", 2); scope.Dispose(); scope.Dispose();
            TowerWorkAccounting.Add("x", 3);
        }
        TowerWorkAccounting.Add("x", 10);
        Assert.Equal(2, inner.Snapshot()["x"]); Assert.Equal(3, outer.Snapshot()["x"]);
        Assert.False(TowerWorkAccounting.Enabled);
    }

    [Fact]
    public async Task Concurrent_contexts_do_not_share_counters()
    {
        async Task<long> Run(int n)
        {
            var ledger = new TowerWorkAccounting(); using var scope = ledger.Activate();
            await Task.Yield(); TowerWorkAccounting.Add("trials", n); return ledger.Snapshot()["trials"];
        }
        Assert.Equal(new long[] { 3, 7 }, await Task.WhenAll(Run(3), Run(7)));
    }

    [Fact]
    public void Counters_reject_negative_or_overflow_and_receipts_require_bindings()
    {
        var ledger = new TowerWorkAccounting(); using var scope = ledger.Activate();
        Assert.Throws<ArgumentOutOfRangeException>(() => TowerWorkAccounting.Add("x", -1));
        TowerWorkAccounting.Add("x", long.MaxValue);
        Assert.Throws<OverflowException>(() => TowerWorkAccounting.Add("x"));
        Assert.Throws<InvalidDataException>(() => ledger.Receipt("unknown", new('a',64), new('b',64), true));
        Assert.Throws<InvalidDataException>(() => ledger.Receipt("native", "", new('b',64), true));
        var receipt = JsonSerializer.SerializeToElement(ledger.Receipt("nativeAudit", new('a',64), new('b',64), false));
        Assert.False(receipt.GetProperty("wholeProcessCoverage").GetBoolean());
        Assert.Equal("Failed", receipt.GetProperty("outcome").GetString());
        Assert.False(receipt.GetProperty("usableForAdmission").GetBoolean());
    }
}
