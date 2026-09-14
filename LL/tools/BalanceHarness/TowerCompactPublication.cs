namespace BalanceHarness;

public sealed record TowerCompactPublication(int ChunkIndex, int PreservedTrials, int ChargedAttempts, string DataHash);

public static partial class TowerCompactBundle
{
    // Only the filesystem rename is retried. Completed fights and their journals are never repeated or rewritten.
    internal static async Task PublishChunkAsync(string output, int index, TowerCompactChunk receipt,
        CancellationToken token, Action<string, string>? move = null)
    {
        output = Path.GetFullPath(output);
        var source = Path.Combine(output, "chunks", ".pending-" + ChunkName(index));
        var destination = Path.Combine(output, "chunks", ChunkName(index));
        move ??= Directory.Move;
        for (var attempt = 0; ; attempt++)
        {
            token.ThrowIfCancellationRequested();
            if (Path.Exists(destination)) throw new InvalidDataException("Publication destination already exists; committed chunks are immutable.");
            if ((File.GetAttributes(Path.Combine(output, "chunks")) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Linked chunk publication directory.");
            var files = Files(source).Select(p => Relative(source, p)).Order(StringComparer.Ordinal).ToArray();
            if (!files.SequenceEqual(new[] { "receipt.json", "records.json.gz" })
                || HarnessJson.Hash(TowerContractJson.Read<TowerCompactChunk>(Path.Combine(source, "receipt.json"))) != HarnessJson.Hash(receipt)
                || HarnessJson.FileHash(Path.Combine(source, "records.json.gz")) != receipt.DataHash)
                throw new InvalidDataException("Pending publication receipt or data changed.");
            try { move(source, destination); return; }
            catch (Exception error) when (attempt < 3 && error is IOException or UnauthorizedAccessException)
            {
                // Revalidate on each attempt; cancellation and persistent failures leave the pending files intact.
                await Task.Delay(50 << attempt, token);
            }
        }
    }

    /// <summary>Explicit zero-combat publication of one complete pending chunk after full prefix verification.</summary>
    public static async Task<TowerCompactPublication> RecoverPublicationAsync(string output, CancellationToken token = default)
    {
        output = Path.GetFullPath(output);
        using var lease = AcquireWriter(output);
        if (File.Exists(Path.Combine(output, ManifestFile))) throw new InvalidDataException("A sealed archive cannot be changed by publication recovery.");
        ReadCheckpoint(output, token); // Includes the link-safe inventory and frozen identity hashes.
        var definition = Definition(output);
        var directories = Directory.GetDirectories(Path.Combine(output, "chunks"));
        var pending = directories.Where(p => Path.GetFileName(p).StartsWith(".pending-", StringComparison.Ordinal)).ToArray();
        var committed = directories.Except(pending).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray();
        var index = committed.Length;
        if (pending.Length != 1 || Path.GetFileName(pending[0]) != ".pending-" + ChunkName(index)
            || !committed.SequenceEqual(Enumerable.Range(0, index).Select(ChunkName)))
            throw new InvalidDataException("Recovery requires exactly the next pending chunk after a contiguous committed prefix.");
        var receipt = TowerContractJson.Read<TowerCompactChunk>(Path.Combine(pending[0], "receipt.json"));
        var completed = checked(index * definition.ChunkSize + receipt.Count);
        var files = Files(output).ToDictionary(p => Relative(output, p), HarnessJson.FileHash, StringComparer.Ordinal);
        // Reuse every normal recipe, content, prepared-report, seed-order, digest and attempt validation before mutation.
        VerifyCore(output, token, null, null, new(1, Format, completed, index + 1, files), pendingChunk: index);
        var attempts = ValidateAttempts(output, definition, completed);
        if (attempts + Validate(definition) - completed > definition.MaximumBattles)
            throw new InvalidDataException("Recovery cannot extend the frozen actual-attempt allowance.");
        await PublishChunkAsync(output, index, receipt, token);
        return new(index, receipt.Count, attempts, receipt.DataHash);
    }
}
