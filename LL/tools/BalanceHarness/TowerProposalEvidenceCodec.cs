using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BalanceHarness;

public sealed record ProposalEvidenceEntry(string LogicalPath, string PhysicalPath, string Codec,
    long LogicalBytes, string LogicalSha256, long PhysicalBytes, string PhysicalSha256);
public sealed record ProposalEvidenceLimits(long LogicalBytes, long PhysicalBytes);
public sealed record ProposalEvidenceRead<T>(T Value, long PhysicalBytesRead, long DecodedBytesProcessed, int DecodePasses);

/// <summary>Opt-in byte codec only. It neither selects a study format nor admits or executes a study.</summary>
public static class TowerProposalEvidenceCodec
{
    public const string Version = "tower-proposal-json-evidence-gzip-v1";
    public const string Codec = "gzip-json-bytes-v1";
    private const int BufferSize = 64 * 1024;
    private static readonly Regex Names = new(@"\A(?:search|pair-(?:0[1-9]|1[0-2])|heldout-(?:0[1-9]|1[0-2])-[0-9a-f]{64})\.json\z", RegexOptions.CultureInvariant);
    private static readonly Regex Digest = new(@"\A[0-9a-f]{64}\z", RegexOptions.CultureInvariant);
    private static readonly uint[] CrcTable = Enumerable.Range(0, 256).Select(i => {
        var c = (uint)i;
        for (var bit = 0; bit < 8; bit++) c = (c & 1) == 0 ? c >> 1 : 0xedb88320U ^ (c >> 1);
        return c;
    }).ToArray();

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }

    private static void Contract(string version, ProposalEvidenceLimits limits)
    {
        Require(version == Version, "Explicit supported evidence format required.");
        Require(limits.LogicalBytes > 0 && limits.PhysicalBytes > 0, "Trusted positive evidence limits required.");
    }

    private static void Name(string name) => Require(name is not null && Names.IsMatch(name), "Unexpected logical evidence name.");

    // Check every existing ancestor as well as the member; a linked directory is also unsafe.
    private static string Member(string root, string leaf)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(root));
        Require(directory.Exists, "Evidence directory does not exist.");
        for (var d = directory; d is not null; d = d.Parent)
            Require((d.Attributes & FileAttributes.ReparsePoint) == 0, "Linked evidence directory.");
        var path = Path.Combine(directory.FullName, leaf);
        if (Path.Exists(path)) Require((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0, "Linked evidence member.");
        return path;
    }

    public static void ValidateEntry(ProposalEvidenceEntry entry, string version, ProposalEvidenceLimits limits)
    {
        Contract(version, limits); Name(entry.LogicalPath);
        Require(entry.PhysicalPath == entry.LogicalPath + ".gz" && entry.Codec == Codec, "Changed evidence mapping or codec.");
        Require(entry.LogicalBytes > 0 && entry.LogicalBytes <= limits.LogicalBytes
            && entry.PhysicalBytes >= 20 && entry.PhysicalBytes <= limits.PhysicalBytes, "Evidence length exceeds trusted limits.");
        Require(entry.LogicalSha256 is not null && Digest.IsMatch(entry.LogicalSha256)
            && entry.PhysicalSha256 is not null && Digest.IsMatch(entry.PhysicalSha256), "Invalid evidence digest.");
    }

    /// <summary>Validate an index against caller-derived membership, never against its own claims.</summary>
    public static void ValidateEntries(IReadOnlyList<ProposalEvidenceEntry> entries, IReadOnlyList<string> expected,
        string version, ProposalEvidenceLimits totals, int maximumMembers)
    {
        Contract(version, totals);
        Require(maximumMembers > 0 && entries.Count <= maximumMembers && entries.Count == expected.Count, "Evidence member limit or membership mismatch.");
        foreach (var name in expected) Name(name);
        Require(expected.Distinct(StringComparer.OrdinalIgnoreCase).Count() == expected.Count, "Repeated expected member.");
        Require(entries.Select(e => e.LogicalPath).SequenceEqual(expected.Order(StringComparer.Ordinal)), "Missing, repeated or unsorted evidence entry.");
        long logical = 0, physical = 0;
        foreach (var entry in entries)
        {
            ValidateEntry(entry, version, totals);
            Require(entry.LogicalBytes <= totals.LogicalBytes - logical && entry.PhysicalBytes <= totals.PhysicalBytes - physical, "Aggregate evidence limit exceeded.");
            logical += entry.LogicalBytes; physical += entry.PhysicalBytes;
        }
    }

    public static ProposalEvidenceEntry WriteNew<T>(string root, string logicalPath, T value, string version,
        ProposalEvidenceLimits limits, Action<long> chargePhysicalBytes, CancellationToken token = default) =>
        WriteRawNew(root, logicalPath, stream => JsonSerializer.Serialize(stream, value, HarnessJson.Options),
            version, limits, chargePhysicalBytes, token);

    internal static ProposalEvidenceEntry WriteRawNew(string root, string logicalPath, Action<Stream> serialize,
        string version, ProposalEvidenceLimits limits, Action<long> chargePhysicalBytes, CancellationToken token = default)
    {
        Contract(version, limits); Name(logicalPath); ArgumentNullException.ThrowIfNull(chargePhysicalBytes);
        token.ThrowIfCancellationRequested();
        Require(!Path.Exists(Member(root, logicalPath)), "Plain and compressed evidence cannot coexist.");
        var path = Member(root, logicalPath + ".gz");
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize);
        using var observed = TowerWorkAccounting.WriteStream(file, path);
        using var physical = new WriteCounter(observed, limits.PhysicalBytes, chargePhysicalBytes, token);
        using var gzip = new GZipStream(physical, CompressionLevel.Optimal, true);
        using var logical = new WriteCounter(gzip, limits.LogicalBytes, _ => { }, token);
        serialize(logical);
        token.ThrowIfCancellationRequested();
        Require(logical.Count > 0, "Empty evidence document.");
        gzip.Dispose(); // Include the trailer before finalizing the physical digest/descriptor.
        TowerWorkAccounting.FlushToDisk(observed); token.ThrowIfCancellationRequested();
        return new(logicalPath, logicalPath + ".gz", Codec, logical.Count, logical.Hash(), physical.Count, physical.Hash());
    }

    public static ProposalEvidenceRead<T> Read<T>(string root, ProposalEvidenceEntry entry, string version,
        ProposalEvidenceLimits limits, CancellationToken token = default)
    {
        ValidateEntry(entry, version, limits); token.ThrowIfCancellationRequested();
        Require(!Path.Exists(Member(root, entry.LogicalPath)), "Plain and compressed evidence cannot coexist.");
        var path = Member(root, entry.PhysicalPath);
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan);
        using var counted = new ReadCounter(file, token);
        Require(file.Length == entry.PhysicalBytes, "Changed physical length.");
        void AuthenticatePhysical()
        {
            counted.Position = 0;
            Require(Convert.ToHexStringLower(SHA256.HashData(counted)) == entry.PhysicalSha256, "Changed physical digest.");
        }
        AuthenticatePhysical();
        counted.Position = 0; Span<byte> header = stackalloc byte[10]; counted.ReadExactly(header);
        Require(header[0] == 31 && header[1] == 139 && header[2] == 8 && header[3] == 0
            && BinaryPrimitives.ReadUInt32LittleEndian(header[4..8]) == 0, "Unsupported gzip header.");
        counted.Position = entry.PhysicalBytes - 8; Span<byte> trailer = stackalloc byte[8]; counted.ReadExactly(trailer);
        var crc = BinaryPrimitives.ReadUInt32LittleEndian(trailer[..4]);
        Require(BinaryPrimitives.ReadUInt32LittleEndian(trailer[4..]) == unchecked((uint)entry.LogicalBytes), "Changed gzip decoded length.");
        var end = entry.PhysicalBytes - 8;

        // DeflateStream may read ahead. First locate the final input window efficiently;
        // the second pass feeds that window bytewise so trailing bytes cannot be hidden
        // in an inflater buffer. Both full decode passes are exposed in accounting.
        counted.Position = 10;
        using var fastInput = new DeflateInput(counted, end, long.MaxValue);
        using var fastInflate = new DeflateStream(fastInput, CompressionMode.Decompress, true);
        using var first = new LogicalRead(fastInflate, entry, crc, token);
        first.CopyTo(Stream.Null, BufferSize); first.Finish();
        var finalWindow = fastInput.LastReadStart;
        counted.Position = 10;
        using var exactInput = new DeflateInput(counted, end, finalWindow);
        using var exactInflate = new DeflateStream(exactInput, CompressionMode.Decompress, true);
        using var second = new LogicalRead(exactInflate, entry, crc, token);
        var options = new JsonSerializerOptions(HarnessJson.Options) { AllowDuplicateProperties = false };
        var value = TowerWorkAccounting.Parse<T>(second, options);
        second.CopyTo(Stream.Null, BufferSize); second.Finish();
        Require(counted.Position == end, "Trailing data or multiple gzip members.");
        AuthenticatePhysical();
        Require(!Path.Exists(Member(root, entry.LogicalPath)), "Plain evidence appeared during verification.");
        _ = Member(root, entry.PhysicalPath);
        return new(value!, counted.Count, checked(first.Count + second.Count), 2);
    }

    private abstract class ForwardStream(Stream inner) : Stream
    {
        protected Stream Inner { get; } = inner;
        public override bool CanRead => Inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => Inner.CanWrite;
        public override long Length => Inner.Length;
        public override long Position { get => Inner.Position; set => throw new NotSupportedException(); }
        public override void Flush() => Inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
        public override int Read(Span<byte> buffer) => Inner.Read(buffer);
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
        public override void Write(ReadOnlySpan<byte> buffer) => Inner.Write(buffer);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }

    private sealed class WriteCounter(Stream inner, long limit, Action<long> charge, CancellationToken token) : ForwardStream(inner)
    {
        private readonly IncrementalHash digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        public long Count { get; private set; }
        public string Hash() => Convert.ToHexStringLower(digest.GetCurrentHash());
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            token.ThrowIfCancellationRequested();
            Require(buffer.Length <= limit - Count, "Streaming evidence byte limit exceeded.");
            charge(buffer.Length); // Charge before dispatch; a failed write never refunds the allowance.
            Inner.Write(buffer); digest.AppendData(buffer); Count += buffer.Length;
        }
        protected override void Dispose(bool disposing) { if (disposing) digest.Dispose(); base.Dispose(disposing); }
    }

    private sealed class ReadCounter(Stream inner, CancellationToken token) : ForwardStream(inner)
    {
        public long Count { get; private set; }
        public override long Position { get => Inner.Position; set => Inner.Position = value; }
        public override int Read(Span<byte> buffer)
        {
            token.ThrowIfCancellationRequested(); var n = Inner.Read(buffer); Count = checked(Count + n);
            TowerWorkAccounting.Add("applicationReadBytes.payload", n); return n;
        }
    }

    private sealed class DeflateInput(Stream inner, long end, long exactFrom) : ForwardStream(inner)
    {
        public long LastReadStart { get; private set; } = 10;
        public override int Read(Span<byte> buffer)
        {
            if (buffer.Length == 0) return 0;
            Require(Inner.Position < end, "Truncated DEFLATE stream.");
            LastReadStart = Inner.Position;
            var count = Math.Min(buffer.Length, end - Inner.Position);
            count = Math.Min(count, Inner.Position >= exactFrom ? 1 : exactFrom - Inner.Position);
            var n = Inner.Read(buffer[..(int)count]); Require(n > 0, "Truncated evidence payload."); return n;
        }
    }

    private sealed class LogicalRead : ForwardStream
    {
        private readonly ProposalEvidenceEntry entry;
        private readonly uint expectedCrc;
        private readonly CancellationToken token;
        public LogicalRead(Stream inner, ProposalEvidenceEntry entry, uint expectedCrc, CancellationToken token) : base(inner)
        {
            this.entry = entry; this.expectedCrc = expectedCrc; this.token = token;
            TowerWorkAccounting.Add("decodePassesStarted");
        }
        private readonly IncrementalHash digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        private readonly Decoder utf8 = new UTF8Encoding(false, true).GetDecoder();
        private readonly char[] characters = new char[BufferSize + 2];
        private uint crc = uint.MaxValue;
        private bool complete;
        private bool verified;
        public long Count { get; private set; }
        public override int Read(Span<byte> buffer)
        {
            token.ThrowIfCancellationRequested();
            if (buffer.Length == 0) return 0;
            // At the declared bound ask for one more byte, so oversized streams fail immediately.
            var capacity = (int)Math.Min(buffer.Length, Math.Min(BufferSize, entry.LogicalBytes - Count + (Count == entry.LogicalBytes ? 1 : 0)));
            var n = Inner.Read(buffer[..capacity]);
            TowerWorkAccounting.Add("decodedBytesProcessed", n);
            if (n == 0)
            {
                utf8.Convert(ReadOnlySpan<byte>.Empty, characters, true, out _, out _, out _);
                complete = true; return 0;
            }
            Require(n <= entry.LogicalBytes - Count, "Decoded evidence exceeds authenticated bound.");
            var bytes = buffer[..n]; digest.AppendData(bytes);
            foreach (var b in bytes) crc = CrcTable[(crc ^ b) & 255] ^ (crc >> 8);
            utf8.Convert(bytes, characters, false, out var used, out _, out _);
            Require(used == n, "Incomplete UTF-8 validation."); Count += n; return n;
        }
        public void Finish()
        {
            Require(complete && Count == entry.LogicalBytes, "Incomplete decoded evidence.");
            Require((crc ^ uint.MaxValue) == expectedCrc, "Changed gzip checksum.");
            Require(Convert.ToHexStringLower(digest.GetCurrentHash()) == entry.LogicalSha256, "Changed logical digest.");
            if (!verified) { verified = true; TowerWorkAccounting.Add("decodePassesCompleted"); }
        }
        protected override void Dispose(bool disposing) { if (disposing) digest.Dispose(); base.Dispose(disposing); }
    }
}
