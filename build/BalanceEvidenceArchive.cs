// Loaded by the PowerShell evidence tools; independent of game code and NuGet.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LegendsLegacy.Tools
{
    public sealed class EvidenceFile
    {
        public string Path { get; set; }
        public long Length { get; set; }
        public string Sha256 { get; set; }
    }

    public sealed class EvidenceIndex
    {
        public int SchemaVersion { get; set; } = 1;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public JsonElement Contract { get; set; }
        public List<EvidenceFile> Files { get; set; } = new List<EvidenceFile>();
    }

    public static class BalanceEvidenceArchive
    {
        private const string IndexName = "package.json";
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions { WriteIndented = false };

        public static string Hash(string file)
        {
            using (var stream = File.OpenRead(file))
                return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }

        private static void CheckName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Contains('\\') || name.Contains(':') ||
                name.Any(char.IsControl) || name.Split('/').Any(s => s.Length == 0 || s == "." || s == ".." ||
                    s.EndsWith(".") || s.EndsWith(" ") || s.IndexOfAny(new[] { '<', '>', '"', '|', '?', '*' }) >= 0 ||
                    Regex.IsMatch(s, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|$)", RegexOptions.IgnoreCase)))
                throw new InvalidDataException("Unsafe package path: " + name);
        }

        public static string Resolve(string root, string name)
        {
            CheckName(name);
            root = System.IO.Path.GetFullPath(root);
            if (Directory.Exists(root)) CheckOrdinary(root);
            var current = root;
            foreach (var part in name.Split('/'))
            {
                current = System.IO.Path.Combine(current, part);
                if (File.Exists(current) || Directory.Exists(current)) CheckOrdinary(current);
            }
            return current;
        }

        private static void CheckOrdinary(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Links/reparse points are not evidence files: " + path);
        }

        private static IEnumerable<string> Walk(string directory)
        {
            CheckOrdinary(directory);
            foreach (var path in Directory.EnumerateFileSystemEntries(directory).OrderBy(p => p, StringComparer.Ordinal))
            {
                CheckOrdinary(path);
                if (Directory.Exists(path))
                {
                    foreach (var nested in Walk(path)) yield return nested;
                }
                else yield return path;
            }
        }

        private static IEnumerable<string> Inputs(string root, string[] includes)
        {
            foreach (var include in includes)
            {
                var path = Resolve(root, include);
                if (Directory.Exists(path))
                    foreach (var file in Walk(path)) yield return file;
                else if (File.Exists(path)) yield return path;
                else throw new FileNotFoundException("Required evidence is missing.", path);
            }
        }

        private static EvidenceFile Copy(Stream input, Stream output, string name)
        {
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                var buffer = new byte[65536];
                long length = 0;
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) != 0)
                {
                    output.Write(buffer, 0, read);
                    hash.AppendData(buffer, 0, read);
                    length += read;
                }
                return new EvidenceFile { Path = name, Length = length,
                    Sha256 = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant() };
            }
        }

        private static void Progress(string action, int count)
        {
            if (count % 10000 == 0) Console.WriteLine(action + ": " + count.ToString("N0") + " files");
        }

        public static EvidenceIndex Create(string root, string archive, string[] includes, string contractJson)
        {
            root = System.IO.Path.GetFullPath(root);
            archive = System.IO.Path.GetFullPath(archive);
            foreach (var include in includes)
            {
                var path = Resolve(root, include);
                if (archive.Equals(path, StringComparison.OrdinalIgnoreCase) ||
                    archive.StartsWith(path + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Archive must be outside its selected inputs.");
            }
            var index = new EvidenceIndex { Contract = JsonSerializer.Deserialize<JsonElement>(contractJson) };
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { IndexName };
            using (var output = new FileStream(archive, FileMode.CreateNew, FileAccess.Write))
            using (var zip = new ZipArchive(output, ZipArchiveMode.Create))
            {
                foreach (var file in Inputs(root, includes))
                {
                    var name = System.IO.Path.GetRelativePath(root, file).Replace('\\', '/');
                    CheckName(name);
                    if (!names.Add(name)) throw new InvalidDataException("Duplicate package input: " + name);
                    var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
                    using (var source = File.OpenRead(file))
                    using (var target = entry.Open()) index.Files.Add(Copy(source, target, name));
                    Progress("Packaged", index.Files.Count);
                }
                // Check source hashes and the complete file set again before sealing the archive.
                VerifyFiles(root, index, Inputs(root, includes));
                using (var target = zip.CreateEntry(IndexName, CompressionLevel.Optimal).Open())
                    JsonSerializer.Serialize(target, index, Json);
            }
            return index;
        }

        private static Dictionary<string, EvidenceFile> FileMap(EvidenceIndex index)
        {
            if (index == null || index.SchemaVersion != 1 || index.Files == null || index.Files.Count == 0)
                throw new InvalidDataException("Unsupported or empty package index.");
            var map = new Dictionary<string, EvidenceFile>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in index.Files)
            {
                CheckName(file.Path);
                if (file.Path.Equals(IndexName, StringComparison.OrdinalIgnoreCase) || file.Length < 0 ||
                    file.Sha256 == null || !Regex.IsMatch(file.Sha256, "^[a-f0-9]{64}$") || !map.TryAdd(file.Path, file))
                    throw new InvalidDataException("Invalid or duplicate file in package index.");
            }
            return map;
        }

        private static void VerifyFiles(string root, EvidenceIndex index, IEnumerable<string> files)
        {
            var remaining = FileMap(index);
            int count = 0;
            foreach (var path in files)
            {
                var name = System.IO.Path.GetRelativePath(root, path).Replace('\\', '/');
                if (!remaining.Remove(name, out var expected)) throw new InvalidDataException("Unexpected file: " + name);
                if (new FileInfo(path).Length != expected.Length || Hash(path) != expected.Sha256)
                    throw new InvalidDataException("Evidence checksum mismatch: " + name);
                Progress("Verified", ++count);
            }
            if (remaining.Count != 0) throw new InvalidDataException("Package files are missing.");
        }

        public static EvidenceIndex Verify(string directory)
        {
            directory = System.IO.Path.GetFullPath(directory);
            using (var source = File.OpenRead(Resolve(directory, IndexName)))
            {
                var index = JsonSerializer.Deserialize<EvidenceIndex>(source, Json);
                VerifyFiles(directory, index, Walk(directory).Where(p =>
                    !System.IO.Path.GetRelativePath(directory, p).Equals(IndexName, StringComparison.Ordinal)));
                return index;
            }
        }

        public static EvidenceIndex Restore(string archive, string expectedSha256, string directory)
        {
            directory = System.IO.Path.GetFullPath(directory);
            if (File.Exists(directory) || Directory.Exists(directory)) throw new IOException("Restore output already exists.");
            if (expectedSha256 == null || !Regex.IsMatch(expectedSha256, "^[a-fA-F0-9]{64}$") ||
                !Hash(archive).Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Archive SHA-256 does not match the expected value.");
            using (var zip = ZipFile.OpenRead(archive))
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in zip.Entries)
                {
                    CheckName(entry.FullName);
                    if (!names.Add(entry.FullName) || ((entry.ExternalAttributes >> 16) & 0xf000) == 0xa000 ||
                        (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0)
                        throw new InvalidDataException("Duplicate or linked archive entry.");
                }
                var indexEntry = zip.GetEntry(IndexName) ?? throw new InvalidDataException("Missing package index.");
                EvidenceIndex index;
                using (var source = indexEntry.Open()) index = JsonSerializer.Deserialize<EvidenceIndex>(source, Json);
                var map = FileMap(index);
                if (zip.Entries.Count != map.Count + 1) throw new InvalidDataException("Archive file set differs from index.");
                foreach (var entry in zip.Entries.Where(e => e.FullName != IndexName))
                    if (!map.TryGetValue(entry.FullName, out var file) || file.Length != entry.Length)
                        throw new InvalidDataException("Archive member differs from index.");
                // All names and the file set are validated before creating any output.
                Directory.CreateDirectory(directory);
                int count = 0;
                foreach (var entry in zip.Entries)
                {
                    var target = Resolve(directory, entry.FullName);
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target));
                    EvidenceFile actual;
                    using (var source = entry.Open())
                    using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write))
                        actual = Copy(source, output, entry.FullName);
                    if (entry.FullName != IndexName && (actual.Sha256 != map[entry.FullName].Sha256 || actual.Length != map[entry.FullName].Length))
                        throw new InvalidDataException("Restored file checksum mismatch: " + entry.FullName);
                    Progress("Restored", ++count);
                }
                return index;
            }
        }
    }
}
