using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record ProposalEvidenceStorage(string Version, long MaximumLogicalBytes, long MaximumPhysicalBytes,
    int MaximumMembers, string CodecSha256, string ReaderSha256);
public sealed record ProposalEvidenceRoot(ProposalEvidenceStorage Selection, IReadOnlyList<string> Indexes);
public sealed record ProposalEvidenceIndex(string Version, IReadOnlyList<ProposalEvidenceEntry> Entries);

/// <summary>Study-scoped storage binding. Scientific plans and logical identities are unchanged.</summary>
public static class TowerProposalEvidenceStorage
{
    public const string IndexName = "evidence-storage.json";
    public static readonly string[] Directories = Enumerable.Range(1, 12)
        .SelectMany(n => new[] { $"search/root-{n:D2}/control/racing", $"search/root-{n:D2}/candidate/racing" })
        .Append("study").Order(StringComparer.Ordinal).ToArray();
    public static readonly string[] Modules = ["proposal_evidence_codec.py", "proposal_evidence_storage.py"];
    private static void Require(bool ok, string reason) { if (!ok) throw new InvalidDataException(reason); }
    public static void Validate(ProposalEvidenceStorage? selection)
    {
        if (selection is null) return;
        Require(selection.Version == TowerProposalEvidenceCodec.Version && selection.MaximumLogicalBytes is > 0 and <= TowerProposalStudy.NativeBytes
            && selection.MaximumPhysicalBytes is > 0 and <= TowerProposalStudy.NativeBytes && selection.MaximumMembers is > 0 and <= 72
            && TowerContractJson.Hash(selection.CodecSha256) && TowerContractJson.Hash(selection.ReaderSha256), "Invalid prospective evidence storage selection.");
    }
    internal static ProposalEvidenceLimits Limits(ProposalEvidenceStorage s) => new(s.MaximumLogicalBytes, s.MaximumPhysicalBytes);
    internal static string[] Names(ProposalStudyFreeze freeze) => Enumerable.Range(1, 12).Select(n => $"pair-{n:D2}.json")
        .Concat(freeze.Families.SelectMany(f => f.Members.Select(m => $"heldout-{f.Root:D2}-{m.RecipeHash}.json"))).Order(StringComparer.Ordinal).ToArray();
    internal static bool Eligible(string name) => name == "search.json" || name.StartsWith("pair-", StringComparison.Ordinal) || name.StartsWith("heldout-", StringComparison.Ordinal);
    internal static T Metadata<T>(string path)
    {
        TowerProposalStudy.Unlinked(path);
        Require(new FileInfo(path).Length <= 65536, "Oversized evidence storage metadata.");
        using var stream = TowerWorkAccounting.ReadStream(File.OpenRead(path), path);
        return TowerWorkAccounting.Parse<T>(stream, new JsonSerializerOptions(HarnessJson.Options) {
            AllowDuplicateProperties = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectRequiredConstructorParameters = true }) ?? throw new InvalidDataException("Empty storage metadata.");
    }
    public static void VerifyModules(string directory, ProposalEvidenceStorage? selection)
    {
        Validate(selection);
        if (selection is null) return;
        foreach (var (name, pin) in Modules.Zip(new[] { selection.CodecSha256, selection.ReaderSha256 }))
        { var path = Path.Combine(directory, name); TowerProposalStudy.Unlinked(path); Require(HarnessJson.FileHash(path) == pin, "Changed retained evidence reader module."); }
    }

    public sealed class Writer
    {
        private readonly string root;
        private readonly ProposalEvidenceStorage selection;
        private readonly Action check;
        private readonly CancellationToken token;
        private readonly Dictionary<string, List<ProposalEvidenceEntry>> entries = new(StringComparer.Ordinal);
        private readonly HashSet<string> sealedDirectories = new(StringComparer.Ordinal);
        private long logical, physical;
        private int members;
        public Writer(string root, ProposalEvidenceStorage selection, Action check, CancellationToken token = default)
        {
            Validate(selection); this.root = Path.GetFullPath(root); this.selection = selection; this.check = check; this.token = token;
            TowerProposalStudy.Unlinked(root); check(); token.ThrowIfCancellationRequested();
            var descriptor = new ProposalEvidenceRoot(selection, Directories.Select(d => d + "/" + IndexName).ToArray());
            var bytes = JsonSerializer.SerializeToUtf8Bytes(descriptor, HarnessJson.Options); Charge(bytes.Length);
            var path = Path.Combine(root, IndexName);
            using var stream = TowerWorkAccounting.WriteStream(new FileStream(path, FileMode.CreateNew, FileAccess.Write), path);
            stream.Write(bytes); TowerWorkAccounting.FlushToDisk(stream);
        }
        private string DirectoryName(string directory)
        {
            var relative = Path.GetRelativePath(root, Path.GetFullPath(directory)).Replace('\\', '/');
            Require(Directories.Contains(relative, StringComparer.Ordinal), "Unexpected evidence directory.");
            Require(!sealedDirectories.Contains(relative), "Evidence index is already sealed."); return relative;
        }
        private void Charge(long bytes)
        {
            token.ThrowIfCancellationRequested(); check();
            Require(bytes <= selection.MaximumPhysicalBytes - physical, "Aggregate encoded storage budget exhausted."); physical += bytes;
        }
        public void Put<T>(string directory, string name, T value, Action<long>? additionalCharge = null)
        {
            var relative = DirectoryName(directory);
            Require(relative == "study" ? name != "search.json" : name == "search.json", "Wrong evidence family for directory.");
            Require(++members <= selection.MaximumMembers, "Encoded member budget exhausted.");
            var entry = TowerProposalEvidenceCodec.WriteNew(directory, name, value, selection.Version,
                new(selection.MaximumLogicalBytes - logical, selection.MaximumPhysicalBytes - physical),
                n => { Charge(n); additionalCharge?.Invoke(n); }, token);
            logical = checked(logical + entry.LogicalBytes);
            if (!entries.TryGetValue(relative, out var list)) entries[relative] = list = [];
            list.Add(entry);
        }
        public void SealDirectory(string directory, IReadOnlyList<string> expected, Action<string, object> save)
        {
            var relative = DirectoryName(directory);
            var list = entries.GetValueOrDefault(relative, []).OrderBy(e => e.LogicalPath, StringComparer.Ordinal).ToArray();
            TowerProposalEvidenceCodec.ValidateEntries(list, expected, selection.Version, Limits(selection), selection.MaximumMembers);
            var index = new ProposalEvidenceIndex(selection.Version, list);
            Charge(JsonSerializer.SerializeToUtf8Bytes(index, HarnessJson.Options).Length);
            save(IndexName, index); sealedDirectories.Add(relative);
        }
        public void Finish() => Require(sealedDirectories.SetEquals(Directories), "Incomplete evidence directory indexes.");
    }

    public sealed class Reader
    {
        private readonly string root;
        private readonly ProposalEvidenceStorage selection;
        private readonly Dictionary<string, Dictionary<string, ProposalEvidenceEntry>> entries = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> metadataPins = new(StringComparer.Ordinal);
        public long PhysicalBytesRead { get; private set; }
        public long DecodedBytesProcessed { get; private set; }
        public int DecodePasses { get; private set; }
        public Reader(string root, ProposalEvidenceStorage selection, ProposalStudyFreeze freeze, CancellationToken token = default)
        {
            Validate(selection); this.root = Path.GetFullPath(root); this.selection = selection;
            var descriptorPath = Path.Combine(root, IndexName);
            var descriptor = Metadata<ProposalEvidenceRoot>(descriptorPath);
            Require(descriptor.Selection == selection && descriptor.Indexes.SequenceEqual(Directories.Select(d => d + "/" + IndexName)), "Changed request/root storage binding.");
            metadataPins[descriptorPath] = HarnessJson.FileHash(descriptorPath);
            long logical = 0, physical = new FileInfo(descriptorPath).Length; int count = 0;
            foreach (var relative in Directories)
            {
                token.ThrowIfCancellationRequested();
                var directory = Path.Combine(root, relative);
                var owner = relative == "study" ? directory : Path.GetDirectoryName(directory)!;
                TowerBulkCampaign.VerifyFiles(owner, "files.json", true, token);
                var path = Path.Combine(directory, IndexName); var index = Metadata<ProposalEvidenceIndex>(path);
                var expected = relative == "study" ? Names(freeze) : ["search.json"];
                Require(index.Version == selection.Version, "Mixed evidence index version.");
                TowerProposalEvidenceCodec.ValidateEntries(index.Entries, expected, index.Version, Limits(selection), selection.MaximumMembers);
                entries[relative] = index.Entries.ToDictionary(e => e.LogicalPath, StringComparer.Ordinal);
                metadataPins[path] = HarnessJson.FileHash(path);
                physical = checked(physical + new FileInfo(path).Length);
                foreach (var entry in index.Entries)
                {
                    logical = checked(logical + entry.LogicalBytes); physical = checked(physical + entry.PhysicalBytes); count++;
                    Require(!Path.Exists(Path.Combine(directory, entry.LogicalPath)), "Mixed plain/compressed evidence.");
                    var payload = Path.Combine(directory, entry.PhysicalPath);
                    Require(new FileInfo(payload).Length == entry.PhysicalBytes && HarnessJson.FileHash(payload) == entry.PhysicalSha256, "Index/manifest payload disagreement.");
                }
            }
            Require(logical <= selection.MaximumLogicalBytes && physical <= selection.MaximumPhysicalBytes && count <= selection.MaximumMembers,
                "Aggregate evidence budget exceeded.");
        }
        private string Relative(string directory)
        {
            var relative = Path.GetRelativePath(root, Path.GetFullPath(directory)).Replace('\\', '/');
            Require(entries.ContainsKey(relative), "Unbound evidence directory."); return relative;
        }
        public IEnumerable<string> PhysicalMembers(string directory, IEnumerable<string> logicalNames)
        {
            var map = entries[Relative(directory)];
            return logicalNames.Select(n => map.TryGetValue(n, out var entry) ? entry.PhysicalPath : n).Append(IndexName);
        }
        public T Read<T>(string directory, string name, CancellationToken token = default)
        {
            var entry = entries[Relative(directory)].GetValueOrDefault(name) ?? throw new InvalidDataException("Unindexed evidence member.");
            var result = TowerProposalEvidenceCodec.Read<T>(directory, entry, selection.Version, Limits(selection), token);
            PhysicalBytesRead = checked(PhysicalBytesRead + result.PhysicalBytesRead);
            DecodedBytesProcessed = checked(DecodedBytesProcessed + result.DecodedBytesProcessed); DecodePasses = checked(DecodePasses + result.DecodePasses);
            return result.Value;
        }
        public void Finish()
        { foreach (var (path, pin) in metadataPins) Require(HarnessJson.FileHash(path) == pin, "Storage metadata changed during audit."); }
    }
    internal static Reader? Open(string root, ProposalEvidenceStorage? selection, CancellationToken token)
    {
        Validate(selection);
        if (selection is not null) return new(root, selection, HarnessJson.Read<ProposalStudyFreeze>(Path.Combine(root, "study/freeze.json")), token);
        Require(!Path.Exists(Path.Combine(root, IndexName)), "Storage descriptor without an explicit request selection."); return null;
    }
}
