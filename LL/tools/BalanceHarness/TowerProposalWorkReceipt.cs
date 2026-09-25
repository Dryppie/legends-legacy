using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalanceHarness;

internal sealed record ProposalWorkerBinding(string Version, string Phase, string StudyRoot,
    string RequestSha256, string ProducerSha256, string AccountingModuleSha256, string ReceiptPath)
{
    private string? publicationPath;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PublicationPath
    {
        get => publicationPath;
        init { publicationPath = value; PublicationPathSpecified = true; }
    }
    internal bool PublicationPathSpecified { get; private init; }
    private string? publicationPersistencePath;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PublicationPersistencePath
    {
        get => publicationPersistencePath;
        init { publicationPersistencePath = value; PublicationPersistencePathSpecified = true; }
    }
    internal bool PublicationPersistencePathSpecified { get; private init; }
    private WorkerSidecarByteLimits? sidecarByteLimits;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WorkerSidecarByteLimits? SidecarByteLimits
    {
        get => sidecarByteLimits;
        init { sidecarByteLimits = value; SidecarByteLimitsSpecified = true; }
    }
    internal bool SidecarByteLimitsSpecified { get; private init; }
    private PendingStorageBudget? pendingStorageBudget;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PendingStorageBudget? PendingStorageBudget
    {
        get => pendingStorageBudget;
        init { pendingStorageBudget = value; PendingStorageBudgetSpecified = true; }
    }
    internal bool PendingStorageBudgetSpecified { get; private init; }
    private ProposalPendingFamiliesBudget? pendingFamiliesBudget;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProposalPendingFamiliesBudget? PendingFamiliesBudget
    {
        get => pendingFamiliesBudget;
        init { pendingFamiliesBudget = value; PendingFamiliesBudgetSpecified = true; }
    }
    internal bool PendingFamiliesBudgetSpecified { get; private init; }
    private NativeLeaseBudget? nativeLeaseBudget;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NativeLeaseBudget? NativeLeaseBudget
    {
        get => nativeLeaseBudget;
        init { nativeLeaseBudget = value; NativeLeaseBudgetSpecified = true; }
    }
    internal bool NativeLeaseBudgetSpecified { get; private init; }
}

/// <summary>Explicit worker receipt boundary including binding authentication.
/// V2 observes publication separately; both receipts retain incomplete coverage.</summary>
internal static class TowerProposalWorkReceipt
{
    internal const string Version = "tower-proposal-worker-binding-v1";
    internal const string PublicationVersion = "tower-proposal-worker-binding-v2";
    internal const string PersistenceVersion = "tower-proposal-worker-binding-v3";
    internal const string SidecarVersion = "tower-proposal-worker-binding-v4";
    internal const string PendingVersion = "tower-proposal-worker-binding-v5";
    internal const string PendingReceiptVersion = "tower-proposal-work-counters-v2";
    internal const string PhasePendingVersion = "tower-proposal-worker-binding-v6";
    internal const string PhasePendingReceiptVersion = "tower-proposal-work-counters-v3";
    internal const string FamilyPendingVersion = "tower-proposal-worker-binding-v7";
    internal const string FamilyPendingReceiptVersion = "tower-proposal-work-counters-v4";
    internal const string LeaseVersion = "tower-proposal-worker-binding-v8";
    internal const string LeaseReceiptVersion = "tower-proposal-work-counters-v5";
    internal static string ProducerPath => typeof(TowerProposalWorkReceipt).Assembly.Location;
    internal static string Phase(string command) => command switch {
        "tower-proposal-study-run" => "native",
        "tower-proposal-study-audit" => "nativeAudit",
        "tower-proposal-study-publication-check" => "publication",
        _ => throw new InvalidDataException("Work receipts require an owned study worker command.")
    };
    private static void Require(bool ok, string message)
    { if (!ok) throw new InvalidDataException(message); }

    internal static async Task<T> Run<T>(string root, string phase, string bindingPath, string bindingPin, Func<Task<T>> action)
    {
        var work = new TowerWorkAccounting();
        using var accounting = work.Activate();
        root = Path.GetFullPath(root);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        void Outside(string path)
        {
            Require(Path.IsPathFullyQualified(path), "Work paths must be absolute.");
            var full = Path.GetFullPath(path);
            Require(!string.Equals(full, root, comparison)
                && !full.StartsWith(Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar, comparison),
                "Work sidecars must be outside the study archive.");
            TowerProposalStudy.Unlinked(full);
        }
        Outside(bindingPath);
        Require(TowerContractJson.Hash(bindingPin) && new FileInfo(bindingPath).Length <= 16384, "Invalid worker binding pin/size.");
        byte[] raw;
        using (var source = TowerWorkAccounting.ReadStream(File.OpenRead(bindingPath), bindingPath))
        using (var buffer = new MemoryStream())
        {
            source.CopyTo(buffer);
            raw = buffer.ToArray();
        }
        Require(Convert.ToHexStringLower(SHA256.HashData(raw)) == bindingPin, "Changed worker binding.");
        TowerWorkAccounting.Add("jsonParseAttempts");
        TowerWorkAccounting.Add("jsonInputBytes", raw.Length);
        var binding = JsonSerializer.Deserialize<ProposalWorkerBinding>(raw, new JsonSerializerOptions(HarnessJson.Options) {
            PropertyNameCaseInsensitive = false, AllowDuplicateProperties = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, RespectRequiredConstructorParameters = true
        }) ?? throw new InvalidDataException("Empty worker binding.");
        TowerWorkAccounting.Add("jsonParseCompleted");
        Require((binding.Version == Version && !binding.PublicationPathSpecified && !binding.PublicationPersistencePathSpecified
                 || binding.Version == PublicationVersion && binding.PublicationPathSpecified && binding.PublicationPath is not null
                    && !binding.PublicationPersistencePathSpecified
                 || binding.Version is PersistenceVersion or SidecarVersion or PendingVersion or PhasePendingVersion or FamilyPendingVersion or LeaseVersion && binding.PublicationPathSpecified && binding.PublicationPath is not null
                    && binding.PublicationPersistencePathSpecified && binding.PublicationPersistencePath is not null)
            && phase is "native" or "nativeAudit" or "publication"
            && binding.Phase == phase && Path.IsPathFullyQualified(binding.StudyRoot)
            && string.Equals(Path.GetFullPath(binding.StudyRoot), root, comparison), "Wrong worker phase/root.");
        Require(binding.Version is SidecarVersion or PendingVersion or PhasePendingVersion or FamilyPendingVersion or LeaseVersion
            ? binding.SidecarByteLimitsSpecified && binding.SidecarByteLimits is not null
            : !binding.SidecarByteLimitsSpecified, "Invalid worker sidecar binding fields.");
        binding.SidecarByteLimits?.Validate();
        Require(binding.Version is PendingVersion or PhasePendingVersion
            ? binding.PendingStorageBudgetSpecified && binding.PendingStorageBudget is not null
            : !binding.PendingStorageBudgetSpecified, "Invalid native pending binding fields.");
        Require(binding.Version is FamilyPendingVersion or LeaseVersion
            ? binding.PendingFamiliesBudgetSpecified && binding.PendingFamiliesBudget is not null
            : !binding.PendingFamiliesBudgetSpecified, "Invalid native pending family fields.");
        Require(binding.Version == LeaseVersion ? binding.NativeLeaseBudgetSpecified && binding.NativeLeaseBudget is not null
            : !binding.NativeLeaseBudgetSpecified, "Invalid native lease fields.");
        var leases = binding.NativeLeaseBudget is null ? null : new TowerNativeLeases(root, phase, binding.NativeLeaseBudget);
        if (binding.Version == PhasePendingVersion) TowerProposalPendingPlan.Validate(root, phase, binding.PendingStorageBudget!);
        var pending = binding.PendingStorageBudget is null ? null : new TowerPendingStorage(binding.PendingStorageBudget,
            allowEmpty: binding.Version == PhasePendingVersion);
        if (binding.Version is FamilyPendingVersion or LeaseVersion) pending = TowerPendingStorage.ForFamilies(new(root, phase, binding.PendingFamiliesBudget!));
        if (binding.Version is PhasePendingVersion or FamilyPendingVersion or LeaseVersion) pending!.ProtectWorkerPaths(TowerProposalPendingPlan.ProtectedPaths(root));
        leases?.ProtectWorkerPaths(TowerProposalPendingPlan.ProtectedPaths(root));
        Require(TowerContractJson.Hash(binding.RequestSha256) && TowerContractJson.Hash(binding.ProducerSha256)
            && binding.AccountingModuleSha256 == binding.ProducerSha256, "Invalid native worker identity.");
        Outside(binding.ReceiptPath);
        if (binding.PublicationPath is { } publicationPath)
        {
            Outside(publicationPath);
            Require(!string.Equals(Path.GetFullPath(publicationPath), Path.GetFullPath(binding.ReceiptPath), comparison),
                "Receipt and publication paths overlap.");
        }
        if (binding.PublicationPersistencePath is { } persistencePath)
        {
            Outside(persistencePath);
            Require(!string.Equals(Path.GetFullPath(persistencePath), Path.GetFullPath(binding.ReceiptPath), comparison)
                && !string.Equals(Path.GetFullPath(persistencePath), Path.GetFullPath(binding.PublicationPath!), comparison),
                "Persistence paths overlap.");
        }
        pending?.ProtectWorkerPaths(bindingPath, Path.Combine(root, "request.json"), ProducerPath,
            binding.ReceiptPath, binding.PublicationPath!, binding.PublicationPersistencePath!);
        leases?.ProtectWorkerPaths(bindingPath, Path.Combine(root, "request.json"), ProducerPath,
            binding.ReceiptPath, binding.PublicationPath!, binding.PublicationPersistencePath!);
        object Receipt(bool succeeded) => leases is not null
            ? new { version = LeaseReceiptVersion, phase, binding.RequestSha256, binding.ProducerSha256,
                outcome = succeeded ? "Complete" : "Failed", coverage = "InstrumentedOperationsOnly", counters = work.Snapshot(),
                wholeProcessCoverage = false, usableForAdmission = false, bindingSha256 = bindingPin,
                pendingStorage = pending!.Snapshot(), nativeLeases = leases.Snapshot() }
            : pending is null
            ? work.Receipt(phase, binding.RequestSha256, binding.ProducerSha256, succeeded)
            : new { version = binding.Version == FamilyPendingVersion ? FamilyPendingReceiptVersion
                    : binding.Version == PhasePendingVersion ? PhasePendingReceiptVersion : PendingReceiptVersion, phase, binding.RequestSha256, binding.ProducerSha256,
                outcome = succeeded ? "Complete" : "Failed", coverage = "InstrumentedOperationsOnly",
                counters = work.Snapshot(), wholeProcessCoverage = false, usableForAdmission = false,
                bindingSha256 = bindingPin, pendingStorage = pending.Snapshot() };
        void Authenticate()
        {
            TowerWorkAccounting.Add("workerAuthenticationsAttempted");
            try
            {
                TowerProposalStudy.Unlinked(Path.Combine(root, "request.json"));
                Require(HarnessJson.FileHash(Path.Combine(root, "request.json")) == binding.RequestSha256
                    && HarnessJson.FileHash(ProducerPath) == binding.ProducerSha256, "Changed worker request/producer.");
            }
            catch { TowerWorkAccounting.Add("workerAuthenticationsFailed"); throw; }
            TowerWorkAccounting.Add("workerAuthenticationsCompleted");
        }
        Authenticate();
        var sidecars = binding.SidecarByteLimits is null ? null : new TowerWorkerSidecars(binding.SidecarByteLimits);
        Stream ReserveSidecar(string role, string path) => sidecars is null
            ? new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)
            : sidecars.Open(role, path, () => new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None));
        Exception? sidecarError = null;
        try
        {
            var persistence = binding.PublicationPersistencePath is null ? null : new TowerReceiptPublication(bindingPin, binding, true);
            // The terminal receipt is reserved before any worker action. Its own
            // persistence is excluded, rather than recursively claiming coverage.
            var terminal = binding.PublicationPersistencePath is null ? null
                : ReserveSidecar("persistence", binding.PublicationPersistencePath);
            Exception? persistenceBoundaryError = null;
            try
            {
                var publication = binding.PublicationPath is null ? null : new TowerReceiptPublication(bindingPin, binding);
                Stream OpenObservation() => ReserveSidecar("publication", binding.PublicationPath!);
                var observation = binding.PublicationPath is null ? null
                    : persistence is null ? OpenObservation() : persistence.Open(OpenObservation);
                Exception? boundaryError = null;
                try
                {
                    Stream OpenReceipt() => ReserveSidecar("receipt", binding.ReceiptPath);
                    var stream = publication is null ? OpenReceipt() : publication.Open(OpenReceipt);
                    Exception? original = null;
                    var succeeded = false;
                    try
                    {
                        Task<T> Body() => pending is null ? action() : pending.RunAsync(action);
                        var result = leases is null ? await Body() : await leases.RunAsync(Body);
                        Authenticate();
                        succeeded = true;
                        return result;
                    }
                    catch (Exception error) { original = error; throw; }
                    finally
                    {
                        accounting.Dispose();
                        try
                        {
                            if (publication is not null)
                                publication.Publish(() => JsonSerializer.SerializeToUtf8Bytes(
                                    Receipt(succeeded), HarnessJson.Options),
                                    TowerWorkerSidecars.Sync);
                            else
                            {
                                using (stream)
                                {
                                    JsonSerializer.Serialize(stream, Receipt(succeeded), HarnessJson.Options);
                                    TowerWorkerSidecars.Sync(stream);
                                }
                            }
                        }
                        catch (Exception publicationError) when (original is not null)
                        {
                            original.Data["WorkReceiptPublicationError"] = publicationError.ToString();
                        }
                    }
                }
                catch (Exception error) { boundaryError = error; throw; }
                finally
                {
                    accounting.Dispose();
                    if (publication is not null)
                        publication.Persist(observation!, TowerWorkerSidecars.Sync, boundaryError, persistence);
                }
            }
            catch (Exception error) { persistenceBoundaryError = error; throw; }
            finally
            {
                accounting.Dispose();
                if (persistence is not null)
                    persistence.Persist(terminal!, TowerWorkerSidecars.Sync, persistenceBoundaryError);
            }
        }
        catch (Exception error) { sidecarError = error; throw; }
        finally { sidecars?.Finish(sidecarError); }
    }
}
