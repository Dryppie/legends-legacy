using System.Text.Json;

namespace BalanceHarness;

internal sealed record TowerSharedExecutableManifest(int SchemaVersion, ExecutionIdentity Execution,
    IReadOnlyDictionary<string, string> Files);
internal sealed record TowerSharedExecutableReference(int SchemaVersion, string Path, string ManifestHash);

/// <summary>A self-contained comparison owns one bundle. Each direct-child stage verifies it independently.</summary>
internal static class TowerSharedExecutable
{
    internal const string Profile = "shared-executable-compact-json-v1";
    internal const string RelativePath = "../executable";
    internal const string Manifest = "shared-executable.json";
    internal const string Reference = "executable-reference.json";
    private static void Require(bool value, string message) => TowerRefinementComparisonModel.Require(value, message);
    internal static void ValidateProfile(string? profile)
        => Require(profile is null or Profile, "Unknown refinement archive profile.");
    internal static IDisposable? Activate(string? profile)
    { ValidateProfile(profile); return profile is null ? null : HarnessJson.UseCompactOutput(); }

    internal static void Create(string root, ExecutionIdentity execution, long maximumBytes,
        Func<string, ExecutionIdentity, long, CancellationToken, IReadOnlyDictionary<string, string>> retain,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); CheckAncestors(root);
        Require(maximumBytes >= 65536 && !Path.Exists(System.IO.Path.Combine(root, "executable"))
            && !Path.Exists(System.IO.Path.Combine(root, Manifest)), "New bounded shared executable required.");
        var files = retain(root, execution, maximumBytes - 65536, token);
        var manifest = new TowerSharedExecutableManifest(1, execution, files);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest, HarnessJson.Options);
        Require(bytes.Length <= 65536 && TowerBulkCampaign.StorageBytes(System.IO.Path.Combine(root, "executable"), token)
            <= maximumBytes - bytes.Length, "Shared executable cap exceeded.");
        token.ThrowIfCancellationRequested(); HarnessJson.WriteNew(System.IO.Path.Combine(root, Manifest), manifest);
        Verify(root, execution, token);
    }

    internal static void Verify(string root, ExecutionIdentity expected, CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); CheckAncestors(root);
        var manifestPath = System.IO.Path.Combine(root, Manifest);
        Require((File.GetAttributes(manifestPath) & FileAttributes.ReparsePoint) == 0, "Linked executable manifest.");
        var saved = TowerContractJson.Read<TowerSharedExecutableManifest>(manifestPath);
        Require(saved.SchemaVersion == 1 && HarnessJson.Hash(saved.Execution) == HarnessJson.Hash(expected)
            && saved.Files.Count > 0 && saved.Files.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() == saved.Files.Count
            && expected.AssemblyHashes.All(p => saved.Files.TryGetValue(p.Key + ".dll", out var h) && h == p.Value), "Changed executable identity.");
        var directory = System.IO.Path.Combine(root, "executable");
        var actual = TowerBulkCampaign.Paths(directory).ToDictionary(p => System.IO.Path.GetRelativePath(directory, p).Replace('\\', '/'), p => p, StringComparer.Ordinal);
        Require(actual.Keys.Order(StringComparer.Ordinal).SequenceEqual(saved.Files.Keys.Order(StringComparer.Ordinal)), "Shared executable membership differs.");
        foreach (var (name, hash) in saved.Files) {
            token.ThrowIfCancellationRequested();
            Require(SafeRelative(name) && TowerContractJson.Hash(hash) && HarnessJson.FileHash(actual[name]) == hash, "Changed shared executable file.");
        }
    }

    internal static void WriteReference(string stage, ExecutionIdentity execution, CancellationToken token)
    {
        CheckAncestors(stage); var parent = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(stage))!;
        Require(!Path.Exists(System.IO.Path.Combine(stage, "executable")), "Stage cannot own and share executable files.");
        Verify(parent, execution, token);
        HarnessJson.WriteNew(System.IO.Path.Combine(stage, Reference), new TowerSharedExecutableReference(1, RelativePath,
            HarnessJson.FileHash(System.IO.Path.Combine(parent, Manifest))));
    }

    internal static void VerifyReference(string stage, ExecutionIdentity execution, CancellationToken token)
    {
        CheckAncestors(stage); var path = System.IO.Path.Combine(stage, Reference);
        Require((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0, "Linked executable reference.");
        var reference = TowerContractJson.Read<TowerSharedExecutableReference>(path);
        Require(reference.SchemaVersion == 1 && reference.Path == RelativePath
            && !Path.Exists(System.IO.Path.Combine(stage, "executable")), "Changed executable reference path.");
        var parent = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(stage))!;
        Require(reference.ManifestHash == HarnessJson.FileHash(System.IO.Path.Combine(parent, Manifest)), "Changed executable manifest binding.");
        Verify(parent, execution, token);
    }

    private static bool SafeRelative(string name) => !System.IO.Path.IsPathRooted(name) && !name.Contains('\\')
        && !name.Contains(':') && name.Split('/').All(p => p is not ("" or "." or ".."));
    private static void CheckAncestors(string path)
    {
        for (var p = System.IO.Path.GetFullPath(path); p is not null; p = System.IO.Path.GetDirectoryName(p))
            Require((File.GetAttributes(p) & FileAttributes.ReparsePoint) == 0, "Linked executable container.");
    }
}
