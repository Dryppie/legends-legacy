using System.Security.Cryptography;
using System.Text.Json;

namespace BalanceHarness;

/// <summary>One receipt's application calls; never physical/durable I/O or full process coverage.</summary>
internal sealed class TowerReceiptPublication(string bindingPin, ProposalWorkerBinding binding, bool observationPersistence = false)
{
    internal const string Version = "tower-proposal-worker-publication-v1";
    internal const string PersistenceVersion = "tower-proposal-worker-observation-persistence-v1";
    private sealed record Failure(string Operation, string Type, string Message);
    private readonly Dictionary<string, long> counts = new(StringComparer.Ordinal);
    private readonly List<Failure> errors = [];
    private Stream? stream;
    private string? receiptPin;
    private long? serializedBytes;

    private void Add(string key, long value = 1) => counts[key] = checked(counts.GetValueOrDefault(key) + value);
    private T Call<T>(string operation, Func<T> action)
    {
        Add(operation + "Attempted");
        try
        {
            var result = action();
            Add(operation + "Completed");
            return result;
        }
        catch (Exception error)
        {
            Add(operation + "Failed");
            errors.Add(new(operation, error.GetType().Name, error.Message));
            throw;
        }
    }
    private void Call(string operation, Action action) => Call(operation, () => { action(); return 0; });

    internal Stream Open(Func<Stream> reserve) => stream = Call("open", reserve);

    // Delegates bind actual serialization and sync boundaries and allow failure
    // fixtures without global hooks or a test mode in the production command.
    internal void Publish(Func<byte[]> serialize, Action<Stream> sync)
    {
        Exception? original = null;
        try
        {
            var raw = Call("serialize", serialize);
            serializedBytes = raw.LongLength;
            receiptPin = Convert.ToHexStringLower(SHA256.HashData(raw));
            try { Call("write", () => stream!.Write(raw, 0, raw.Length)); }
            catch { Add("failedWriteBytesUnknown"); throw; }
            // Stream.Write returns no short count: only a completed call accepts
            // the full requested buffer. Thrown calls retain unknown progress.
            Add("acceptedWriteBytes", raw.LongLength);
            Call("flush", stream!.Flush);
            Call("sync", () => sync(stream));
        }
        catch (Exception error) { original = error; throw; }
        finally
        {
            try { Call("close", stream!.Dispose); }
            catch (Exception error) when (original is not null)
            { original.Data["WorkReceiptCloseError"] = error.ToString(); }
        }
    }

    internal object Observation()
    {
        if (observationPersistence) return new {
            version = PersistenceVersion, bindingSha256 = bindingPin, phase = binding.Phase,
            requestSha256 = binding.RequestSha256, producerSha256 = binding.ProducerSha256,
            outcome = errors.Count == 0 && counts.GetValueOrDefault("closeCompleted") == 1 ? "Complete" : "Failed",
            serializedObservationSha256 = receiptPin, serializedObservationBytes = serializedBytes,
            counters = new SortedDictionary<string, long>(counts, StringComparer.Ordinal), errors = errors.ToArray(),
            coverage = "PublicationObservationPersistenceApplicationCalls", boundary = "AfterPublicationObservationAttempt",
            terminalObservationPersistenceExcluded = true, wholeProcessCoverage = false, usableForAdmission = false
        };
        return new {
            version = Version, bindingSha256 = bindingPin, phase = binding.Phase,
            requestSha256 = binding.RequestSha256, producerSha256 = binding.ProducerSha256,
            outcome = errors.Count == 0 && counts.GetValueOrDefault("closeCompleted") == 1 ? "Complete" : "Failed",
            serializedReceiptSha256 = receiptPin, serializedReceiptBytes = serializedBytes,
            counters = new SortedDictionary<string, long>(counts, StringComparer.Ordinal), errors = errors.ToArray(),
            coverage = "ReceiptPublicationApplicationCalls", boundary = "AfterReceiptPublicationAttempt",
            observationPersistenceExcluded = true, wholeProcessCoverage = false, usableForAdmission = false
        };
    }

    internal void Persist(Stream output, Action<Stream> sync, Exception? workerError, TowerReceiptPublication? persistence = null)
    {
        try
        {
            if (persistence is not null)
            {
                persistence.Publish(() => JsonSerializer.SerializeToUtf8Bytes(Observation(), HarnessJson.Options), sync);
                return;
            }
            Exception? original = null;
            try
            {
                JsonSerializer.Serialize(output, Observation(), HarnessJson.Options);
                output.Flush();
                sync(output);
            }
            catch (Exception error) { original = error; throw; }
            finally
            {
                try { output.Dispose(); }
                catch (Exception error) when (original is not null)
                { original.Data["PublicationObservationCloseError"] = error.ToString(); }
            }
        }
        catch (Exception error) when (workerError is not null)
        { workerError.Data[observationPersistence ? "TerminalPublicationPersistenceError" : "PublicationObservationPersistenceError"] = error.ToString(); }
    }
}
