using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessEvidenceCodecTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ll-evidence-codec-" + Guid.NewGuid().ToString("N"));
    private static readonly ProposalEvidenceLimits Limits = new(4 * 1024 * 1024, 4 * 1024 * 1024);
    private const string Version = TowerProposalEvidenceCodec.Version;
    public BalanceHarnessEvidenceCodecTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);
    private static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    private ProposalEvidenceEntry Raw(byte[] bytes) => TowerProposalEvidenceCodec.WriteRawNew(root, "search.json",
        stream => stream.Write(bytes), Version, Limits, _ => { });
    private ProposalEvidenceRead<JsonElement> Read(ProposalEvidenceEntry e, ProposalEvidenceLimits? limits = null) =>
        TowerProposalEvidenceCodec.Read<JsonElement>(root, e, Version, limits ?? Limits);
    private ProposalEvidenceEntry Repin(ProposalEvidenceEntry e, byte[] bytes)
    {
        File.WriteAllBytes(Path.Combine(root, e.PhysicalPath), bytes);
        return e with { PhysicalBytes = bytes.LongLength, PhysicalSha256 = Sha(bytes) };
    }
    private static void Reject(Action action)
    {
        var exception = Assert.ThrowsAny<Exception>(action);
        Assert.True(exception is InvalidDataException or JsonException or DecoderFallbackException, exception.ToString());
    }

    [Fact]
    public void Native_writer_preserves_the_existing_serializer_and_reports_actual_work()
    {
        var value = new { Text = "Æ 🐉 \"\\\n", Numbers = new[] { -1.25, 100.0 }, Nested = new { Empty = new object[0] } };
        var plain = Path.Combine(root, "expected.json"); HarnessJson.WriteNew(plain, value);
        long charged = 0;
        var e = TowerProposalEvidenceCodec.WriteNew(root, "search.json", value, Version, Limits, n => charged += n);
        Assert.Equal(HarnessJson.FileHash(plain), e.LogicalSha256);
        Assert.Equal(new FileInfo(plain).Length, e.LogicalBytes);
        Assert.Equal(e.PhysicalBytes, charged);
        var read = Read(e);
        Assert.Equal(value.Text, read.Value.GetProperty("text").GetString());
        Assert.Equal(2, read.DecodePasses);
        Assert.Equal(2 * e.LogicalBytes, read.DecodedBytesProcessed);
        Assert.True(read.PhysicalBytesRead >= 2 * e.PhysicalBytes);
    }

    [Fact]
    public void Python_gzip_bytes_are_independently_authenticated_without_reserializing_json()
    {
        var logical = Encoding.UTF8.GetBytes(" {\"text\":\"Æ 🐉\",\"number\":1e+2,\"nested\":[null,true,{}]}\n");
        // Produced by Python gzip.compress(logical, compresslevel=6, mtime=0).
        var physical = Convert.FromBase64String("H4sIAAAAAAAAClOoVipJrShRslI63KbwYf6ETiUdpbzS3KTUIiUrw1RtIyAvtbgkNUXJKjqvNCdHp6SoNFWnuja2lgsALDah0joAAAA=");
        var e = new ProposalEvidenceEntry("search.json", "search.json.gz", TowerProposalEvidenceCodec.Codec,
            logical.Length, Sha(logical), physical.Length, Sha(physical));
        File.WriteAllBytes(Path.Combine(root, e.PhysicalPath), physical);
        Assert.Equal("1e+2", Read(e).Value.GetProperty("number").GetRawText());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData(" {\"n\":-0.00e+10,\"x\":\"\\u00c6\"} \n")]
    public void Exact_json_byte_forms_round_trip(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json); var e = Raw(bytes);
        Assert.Equal(Sha(bytes), e.LogicalSha256); Assert.Equal(2 * bytes.Length, Read(e).DecodedBytesProcessed);
    }

    [Fact]
    public void Large_unicode_split_at_buffer_boundaries_round_trips()
    {
        var bytes = Encoding.UTF8.GetBytes("\"" + new string('a', 65532) + string.Concat(Enumerable.Repeat("🐉æ", 30000)) + "\"");
        var e = Raw(bytes);
        Assert.Equal(JsonSerializer.Deserialize<string>(bytes), Read(e).Value.GetString());
    }

    [Theory]
    [InlineData("digest")]
    [InlineData("physical-length")]
    [InlineData("logical-digest")]
    [InlineData("logical-length")]
    [InlineData("codec")]
    [InlineData("mapping")]
    public void Tampered_descriptors_fail(string field)
    {
        var e = Raw("{}"u8.ToArray());
        e = field switch {
            "digest" => e with { PhysicalSha256 = new string('0', 64) },
            "physical-length" => e with { PhysicalBytes = e.PhysicalBytes + 1 },
            "logical-digest" => e with { LogicalSha256 = new string('0', 64) },
            "logical-length" => e with { LogicalBytes = e.LogicalBytes + 1 },
            "codec" => e with { Codec = "plain" },
            _ => e with { PhysicalPath = "../search.json.gz" }
        };
        Reject(() => Read(e));
    }

    [Theory]
    [InlineData("crc")]
    [InlineData("truncated")]
    [InlineData("trailing")]
    [InlineData("member")]
    [InlineData("flag")]
    [InlineData("timestamp")]
    [InlineData("deflate-tail")]
    public void Malformed_gzip_is_rejected_even_with_a_matching_physical_digest(string fault)
    {
        var e = Raw("{\"value\":123}"u8.ToArray()); var bytes = File.ReadAllBytes(Path.Combine(root, e.PhysicalPath));
        switch (fault)
        {
            case "crc": bytes[^8] ^= 1; break;
            case "truncated": bytes = bytes[..^9].Concat(bytes[^8..]).ToArray(); break;
            case "trailing": bytes = bytes.Concat(new byte[] { 0 }).ToArray(); break;
            case "member": bytes = bytes.Concat(bytes).ToArray(); break;
            case "flag": bytes[3] = 8; break;
            case "timestamp": bytes[4] = 1; break;
            case "deflate-tail": bytes = bytes[..^8].Concat(new byte[] { 0 }).Concat(bytes[^8..]).ToArray(); break;
        }
        Reject(() => Read(Repin(e, bytes)));
    }

    [Theory]
    [InlineData("{\"x\":1,\"x\":2}")]
    [InlineData("{\"nested\":{\"x\":1,\"\\u0078\":2}}")]
    [InlineData("{} {}")]
    [InlineData("{\"x\":NaN}")]
    [InlineData(" ")]
    public void Invalid_or_ambiguous_json_is_rejected(string text) => Reject(() => Read(Raw(Encoding.UTF8.GetBytes(text))));

    [Fact]
    public void Invalid_utf8_is_rejected_before_parsing() => Reject(() => Read(Raw([34, 0xc3, 34])));

    [Fact]
    public void Trusted_decoded_limit_blocks_highly_compressible_payload_before_opening_it()
    {
        var e = Raw(Encoding.UTF8.GetBytes("\"" + new string('x', 1000000) + "\""));
        Reject(() => Read(e, Limits with { LogicalBytes = 1024 }));
        // Even a repinned descriptor/trailer claiming a small length cannot bypass streaming limits.
        var physical = File.ReadAllBytes(Path.Combine(root, e.PhysicalPath));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(physical.AsSpan(physical.Length - 4), 20);
        e = Repin(e with { LogicalBytes = 20 }, physical);
        Reject(() => Read(e));
    }

    [Fact]
    public void Writer_limits_and_failed_charge_leave_partial_bytes_without_a_descriptor()
    {
        Reject(() => TowerProposalEvidenceCodec.WriteNew(root, "search.json", new string('x', 1000), Version,
            Limits with { LogicalBytes = 10 }, _ => { }));
        Assert.True(File.Exists(Path.Combine(root, "search.json.gz")));
        Assert.False(File.Exists(Path.Combine(root, "evidence-storage.json")));
        Assert.Throws<IOException>(() => Raw("{}"u8.ToArray()));
    }

    [Fact]
    public void Cancellation_precedes_creation_and_is_checked_during_read()
    {
        var token = new CancellationToken(true);
        Assert.Throws<OperationCanceledException>(() => TowerProposalEvidenceCodec.WriteNew(root, "search.json", new { }, Version, Limits, _ => { }, token));
        Assert.Empty(Directory.GetFiles(root));
        var e = Raw("{}"u8.ToArray());
        Assert.Throws<OperationCanceledException>(() => TowerProposalEvidenceCodec.Read<JsonElement>(root, e, Version, Limits, token));
    }

    [Theory]
    [InlineData("../search.json")]
    [InlineData("SEARCH.json")]
    [InlineData("pair-13.json")]
    [InlineData("plan.json")]
    [InlineData("search.json:stream")]
    public void Unsafe_or_ineligible_names_fail_before_creation(string name)
    {
        Reject(() => TowerProposalEvidenceCodec.WriteNew(root, name, new { }, Version, Limits, _ => { }));
        Assert.Empty(Directory.GetFiles(root));
    }

    [Fact]
    public void Explicit_format_and_plain_collision_are_required()
    {
        Reject(() => TowerProposalEvidenceCodec.WriteNew(root, "search.json", new { }, "unknown", Limits, _ => { }));
        File.WriteAllText(Path.Combine(root, "search.json"), "{}");
        Reject(() => Raw("{}"u8.ToArray()));
        Assert.False(File.Exists(Path.Combine(root, "search.json.gz")));
    }

    [Fact]
    public void Index_membership_order_and_aggregate_limits_are_independently_enforced()
    {
        var e = Raw("{}"u8.ToArray());
        TowerProposalEvidenceCodec.ValidateEntries([e], ["search.json"], Version, Limits, 1);
        Reject(() => TowerProposalEvidenceCodec.ValidateEntries([e, e], ["search.json", "search.json"], Version, Limits, 2));
        Reject(() => TowerProposalEvidenceCodec.ValidateEntries([e], ["pair-01.json"], Version, Limits, 1));
        Reject(() => TowerProposalEvidenceCodec.ValidateEntries([e], ["search.json"], Version, Limits, 0));
        var pair = e with { LogicalPath = "pair-01.json", PhysicalPath = "pair-01.json.gz" };
        Reject(() => TowerProposalEvidenceCodec.ValidateEntries([e, pair], ["search.json", "pair-01.json"], Version, Limits, 2));
        Reject(() => TowerProposalEvidenceCodec.ValidateEntries([pair, e], ["pair-01.json", "search.json"], Version,
            Limits with { LogicalBytes = 3 }, 2));
    }

    [Fact]
    public void Long_compressed_input_enforces_the_exact_end_after_read_ahead()
    {
        var bytes = new byte[100000]; new Random(120).NextBytes(bytes);
        var text = Convert.ToBase64String(bytes);
        var e = Raw(Encoding.UTF8.GetBytes("\"" + text + "\""));
        Assert.True(e.PhysicalBytes > 65536);
        Assert.Equal(text, Read(e).Value.GetString());
        var physical = File.ReadAllBytes(Path.Combine(root, e.PhysicalPath));
        Reject(() => Read(Repin(e, physical[..^8].Concat(new byte[] { 0, 0, 0 }).Concat(physical[^8..]).ToArray())));
    }

    [Fact]
    public void Physical_limit_or_charge_failure_cannot_return_a_published_descriptor()
    {
        Reject(() => TowerProposalEvidenceCodec.WriteNew(root, "search.json", new { }, Version,
            Limits with { PhysicalBytes = 10 }, _ => { }));
        Assert.True(File.Exists(Path.Combine(root, "search.json.gz")));
        Assert.Throws<InvalidOperationException>(() => TowerProposalEvidenceCodec.WriteNew(root, "pair-01.json", new { }, Version,
            Limits, _ => throw new InvalidOperationException("charge refused")));
        Assert.True(File.Exists(Path.Combine(root, "pair-01.json.gz")));
    }

    [Fact]
    public void Codec_keeps_plain_reader_and_compact_serializer_options_unchanged()
    {
        var options = HarnessJson.Options; var value = new { Text = "legacy", Ids = new[] { 1, 2 } };
        using (HarnessJson.UseCompactOutput())
        {
            var compact = HarnessJson.Options;
            var plain = Path.Combine(root, "plain.json"); HarnessJson.WriteNew(plain, value);
            var e = TowerProposalEvidenceCodec.WriteNew(root, "search.json", value, Version, Limits, _ => { });
            Assert.Equal(HarnessJson.FileHash(plain), e.LogicalSha256);
            Assert.Equal(HarnessJson.Hash(HarnessJson.Read<JsonElement>(plain)), HarnessJson.Hash(Read(e).Value));
            Assert.Same(compact, HarnessJson.Options);
        }
        Assert.Same(options, HarnessJson.Options);
    }

    [Fact]
    public void Native_samples_can_be_retained_for_the_independent_python_reader()
    {
        var samples = new[] { "{}", " {\"text\":\"Æ 🐉\",\"number\":1e+2,\"nested\":[null,true,{}]}\n",
            "\"" + new string('a', 65532) + string.Concat(Enumerable.Repeat("🐉æ", 30000)) + "\"" };
        var exchange = Environment.GetEnvironmentVariable("LL_EVIDENCE_CODEC_EXCHANGE");
        for (var i = 0; i < samples.Length; i++)
        {
            var folder = Path.Combine(root, i.ToString()); Directory.CreateDirectory(folder);
            var bytes = Encoding.UTF8.GetBytes(samples[i]);
            var entry = TowerProposalEvidenceCodec.WriteRawNew(folder, "search.json", s => s.Write(bytes), Version, Limits, _ => { });
            Assert.Equal(Sha(bytes), entry.LogicalSha256);
            _ = TowerProposalEvidenceCodec.Read<JsonElement>(folder, entry, Version, Limits);
            if (exchange is not null)
            {
                var target = Path.Combine(Path.GetFullPath(exchange), i.ToString()); Directory.CreateDirectory(target);
                File.Copy(Path.Combine(folder, entry.PhysicalPath), Path.Combine(target, entry.PhysicalPath), false);
                HarnessJson.WriteNew(Path.Combine(target, "entry.json"), entry);
                File.WriteAllBytes(Path.Combine(target, "expected.json"), bytes);
            }
        }
    }
}
