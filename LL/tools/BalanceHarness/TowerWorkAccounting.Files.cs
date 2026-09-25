using System.Text;

namespace BalanceHarness;

internal sealed partial class TowerWorkAccounting
{
    // Keep File.ReadAllBytes' sharing, allocation and failure behavior. A thrown
    // call exposes no returned prefix: its read progress remains unknown.
    public static byte[] ReadAllBytes(string path)
    {
        var owner = Active.Value;
        owner?.Record("byteReadOperationsAttempted", 1);
        try
        {
            var bytes = File.ReadAllBytes(path);
            owner?.Record("applicationReadBytes." + Role(path), bytes.LongLength);
            owner?.Record("byteReadOperationsCompleted", 1);
            return bytes;
        }
        catch
        {
            owner?.Record("byteReadOperationsFailed", 1);
            owner?.Record("failedByteReadBytesUnknown", 1);
            throw;
        }
    }

    // Preserve the native APIs: their encoders, metadata handling, sharing and failure behavior
    // are part of the existing archive contract. File.Copy is not a stream I/O observation.
    public static void AppendAllText(string path, string? contents) => WriteText(path, contents, append: true);
    public static void WriteAllText(string path, string? contents) => WriteText(path, contents, append: false);
    private static void WriteText(string path, string? contents, bool append)
    {
        var owner = Active.Value;
        if (owner is not null)
        {
            owner.ObservePath(path);
            owner.Record("textWriteOperationsAttempted", 1);
        }
        try
        {
            if (append) File.AppendAllText(path, contents); else File.WriteAllText(path, contents);
            if (owner is not null)
            {
                owner.Record("applicationWriteBytes." + Role(path), contents is null ? 0 : Encoding.UTF8.GetByteCount(contents));
                owner.Record("textWriteOperationsCompleted", 1);
            }
        }
        catch
        {
            owner?.Record("textWriteOperationsFailed", 1);
            owner?.Record("failedTextWriteBytesUnknown", 1);
            throw;
        }
        finally { owner?.ObservePath(path); }
    }

    public static void CopyFile(string source, string destination, bool overwrite = false)
    {
        var owner = Active.Value;
        if (owner is not null)
        {
            owner.ObservePath(destination);
            owner.Record("fileCopyOperationsAttempted", 1);
        }
        try
        {
            File.Copy(source, destination, overwrite);
            if (owner is not null)
            {
                owner.Record("fileCopyOperationsCompleted", 1);
                try { owner.Record("fileCopyLogicalBytes." + Role(destination), new FileInfo(destination).Length); }
                catch { owner.Record("fileCopyLengthObservationFailures", 1); }
            }
        }
        catch
        {
            owner?.Record("fileCopyOperationsFailed", 1);
            owner?.Record("failedFileCopyBytesUnknown", 1);
            throw;
        }
        finally { owner?.ObservePath(destination); }
    }

    private void ObservePath(string path) => ObserveLength(path, false, () =>
    {
        try { return new FileInfo(path).Length; }
        catch (FileNotFoundException) { return 0; }
        catch (DirectoryNotFoundException) { return 0; }
    });

    // Compact publication moves only an already verified pending directory. Reclassify known
    // members after the original rename callback succeeds; retries never add write bytes.
    public static void PublishDirectory(string source, string destination, Action<string, string> move)
    {
        var owner = Active.Value;
        try { TowerPendingStorage.PublishDirectory(source, destination, move); }
        catch { owner?.Record("directoryPublicationFailures", 1); throw; }
        if (owner is null) return;
        var prefix = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source)) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(destination);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        lock (owner.counters)
        {
            var members = owner.writtenFiles.Where(p => p.Key.StartsWith(prefix, comparison)).ToArray();
            foreach (var (path, file) in members)
            {
                owner.writtenFiles.Remove(path);
                owner.AdjustStorage(file, -1);
                var publishedPath = Path.Combine(target, path[prefix.Length..]);
                if (owner.writtenFiles.Remove(publishedPath, out var previous)) owner.AdjustStorage(previous, -1);
                var published = file with { Scratch = false };
                owner.writtenFiles[publishedPath] = published;
                owner.AdjustStorage(published, 1);
            }
            owner.Record(members.Length == 0 ? "untrackedDirectoryPublications" : "trackedDirectoryPublications", 1);
            owner.UpdateStorage();
        }
    }
}
