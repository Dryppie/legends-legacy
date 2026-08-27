using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Application.Interfaces.Services.LL.Combat;
using Application.Interfaces.Services.LL.CombatProfiles;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Regions;
using Application.Interfaces.Services.LL.WorldTower;
using Domain.Models.Entities.Creatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Profiles;
using Services.LL.PowerRatings;

namespace Services.AdminDashboard.Combat;

public enum WorldTowerAuditCampaignStatus
{
    Queued,
    RunningAudits,
    GeneratingCatalog,
    RunningCandidateSmoke,
    RunningCandidateCertification,
    Completed,
    Failed,
    Cancelled
}

public enum WorldTowerAuditWorkStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public sealed record WorldTowerAuditCampaignOptions(
    int MinimumFloor = 1,
    int MaximumFloor = 15,
    int CandidatePoolSize = 500,
    int ScreeningBattleCount = 10_000,
    int FinalistCount = 24,
    int FinalistBattleCount = 34,
    int ValidationBattleCount = 100,
    IReadOnlyList<int>? RandomSeeds = null,
    int TeamsPerFamily = 1,
    int ProfileRandomSeed = 1337,
    int MinimumSourceBattles = 100,
    int MinimumMatchupBattles = 100,
    double MaximumConfidenceWidth95 = 0.25d,
    double MaximumSeedScoreSpread = 0.15d,
    double MaximumEssenceOverlap = 0.80d,
    bool RequireMultiSeedStability = true,
    int DiscoveryEquipmentTier = 1,
    string DiscoveryEquipmentRarity = "Epic",
    string DiscoveryEquipmentProfile = "Balanced",
    bool RunCandidateVerification = true,
    int SmokeSampleCount = 10,
    int CertificationSampleCount = 100);

public sealed record WorldTowerAuditCampaignScenario(
    WorldTowerProfileScenarioRequirement Requirement,
    string AuditWorkId);

public sealed record WorldTowerAuditCampaignWork(
    string Id,
    string Description,
    AbilityBalanceAuditRequest Request,
    IReadOnlyList<string> ScenarioIds,
    WorldTowerAuditWorkStatus Status,
    int AttemptCount = 0,
    DateTimeOffset? StartedAtUtc = null,
    DateTimeOffset? CompletedAtUtc = null,
    long? TotalBattlesRun = null,
    string? ContentHash = null,
    string? Error = null,
    Guid? ReusedFromCampaignId = null,
    string? ReusedSourceContentHash = null);

public sealed record WorldTowerAuditCampaign(
    int SchemaVersion,
    Guid Id,
    WorldTowerAuditCampaignStatus Status,
    WorldTowerAuditCampaignOptions Options,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<WorldTowerAuditCampaignScenario> Scenarios,
    IReadOnlyList<WorldTowerAuditCampaignWork> Audits,
    bool CancelRequested = false,
    bool CatalogIsValid = false,
    int CatalogProfileSetCount = 0,
    int CatalogIssueCount = 0,
    string? CatalogContentHash = null,
    string? Error = null,
    string? DiscoveryFingerprint = null,
    string? MaterializationFingerprint = null,
    Guid? ReusedCatalogFromCampaignId = null,
    int ReusedAuditCount = 0,
    bool CandidateSmokePassed = false,
    bool CandidateCertificationCompleted = false,
    bool CandidateCertificationPassed = false,
    int CandidateCertificationIssueCount = 0)
{
    public int CompletedAuditCount => Audits.Count(audit =>
        audit.Status == WorldTowerAuditWorkStatus.Completed);
    public int TotalAuditCount => Audits.Count;
    public string? CurrentAuditId => Audits.FirstOrDefault(audit =>
        audit.Status == WorldTowerAuditWorkStatus.Running)?.Id;
    public bool IsPromotionReady => CatalogIsValid
                                    && CandidateSmokePassed
                                    && CandidateCertificationCompleted
                                    && CandidateCertificationPassed;
}

public sealed record WorldTowerAuditCampaignEvidence(
    WorldTowerAuditCampaign Campaign,
    IReadOnlyDictionary<string, AbilityBalanceAuditReport> AuditReports,
    CombatCharacterProfileCatalogValidationReport? CatalogValidation,
    WorldTowerProfileShadowCalibrationReport? CandidateSmoke = null,
    WorldTowerCalibrationCertificationReport? CandidateCertification = null);

public interface IWorldTowerAuditCampaignService
{
    Task<IReadOnlyList<WorldTowerAuditCampaign>> ListAsync(CancellationToken cancellationToken);
    Task<WorldTowerAuditCampaign?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<WorldTowerAuditCampaign> CreateAsync(
        WorldTowerAuditCampaignOptions options,
        CancellationToken cancellationToken);
    Task<WorldTowerAuditCampaign?> CancelAsync(Guid id, CancellationToken cancellationToken);
    Task<WorldTowerAuditCampaign?> RetryAsync(Guid id, CancellationToken cancellationToken);
    Task<CombatCharacterProfileCatalogDocument?> GetCatalogAsync(
        Guid id,
        CancellationToken cancellationToken);
    Task<WorldTowerAuditCampaignEvidence?> GetEvidenceAsync(
        Guid id,
        CancellationToken cancellationToken);
}

public static class WorldTowerAuditCampaignPlanner
{
    public static (
        IReadOnlyList<WorldTowerAuditCampaignScenario> Scenarios,
        IReadOnlyList<WorldTowerAuditCampaignWork> Audits) Create(
        IReadOnlyList<WorldTowerProfileScenarioRequirement> requirements,
        WorldTowerAuditCampaignOptions options)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        ArgumentNullException.ThrowIfNull(options);
        var seeds = options.RandomSeeds ?? [1337, 2027, 9001];
        var distinctSeedCount = seeds.Where(seed => seed != 0).Distinct().Count();
        if (distinctSeedCount == 0)
            distinctSeedCount = 3;
        var matchupBattles = checked((long)options.FinalistBattleCount * distinctSeedCount);
        if (matchupBattles < options.MinimumMatchupBattles)
        {
            throw new ArgumentException(
                $"The campaign can produce only {matchupBattles} battles per finalist matchup, "
                + $"but profile generation requires {options.MinimumMatchupBattles}. "
                + "Increase finalist battles or add seeds before starting the campaign.",
                nameof(options));
        }
        var grouped = requirements
            .GroupBy(requirement => AuditSignature(requirement, options), StringComparer.Ordinal)
            .ToArray();
        var audits = grouped.Select(group =>
        {
            var requirement = group.First();
            var id = $"audit-{ShortHash(group.Key)}";
            return new WorldTowerAuditCampaignWork(
                id,
                $"5-member party discovery, {requirement.EssencesPerParticipant} Essences",
                new AbilityBalanceAuditRequest(
                    5,
                    requirement.EssencesPerParticipant,
                    options.CandidatePoolSize,
                    options.ScreeningBattleCount,
                    options.FinalistCount,
                    options.FinalistBattleCount,
                    options.ValidationBattleCount,
                    seeds,
                    options.DiscoveryEquipmentTier,
                    options.DiscoveryEquipmentRarity,
                    options.DiscoveryEquipmentProfile,
                    UseCanonicalRoles: true),
                group.Select(item => item.ScenarioId).Order(StringComparer.Ordinal).ToArray(),
                WorldTowerAuditWorkStatus.Queued);
        }).OrderBy(audit => audit.Description, StringComparer.Ordinal).ToArray();
        var workBySignature = grouped.ToDictionary(
            group => group.Key,
            group => $"audit-{ShortHash(group.Key)}",
            StringComparer.Ordinal);
        var scenarios = requirements.Select(requirement => new WorldTowerAuditCampaignScenario(
            requirement,
            workBySignature[AuditSignature(requirement, options)])).ToArray();
        return (scenarios, audits);
    }

    private static string AuditSignature(
        WorldTowerProfileScenarioRequirement requirement,
        WorldTowerAuditCampaignOptions options) =>
        string.Join('|',
            5,
            options.DiscoveryEquipmentTier,
            options.DiscoveryEquipmentRarity.ToLowerInvariant(),
            options.DiscoveryEquipmentProfile.ToLowerInvariant(),
            requirement.EssencesPerParticipant);

    private static string ShortHash(string value) => Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(value)))
        .ToLowerInvariant()[..16];
}

public enum WorldTowerBalancingReuseMode
{
    RunDiscovery,
    RebuildProfiles,
    ReuseProfiles
}

public static class WorldTowerBalancingDependencyPlanner
{
    public static WorldTowerBalancingReuseMode Decide(
        string? previousDiscoveryFingerprint,
        string? previousMaterializationFingerprint,
        string currentDiscoveryFingerprint,
        string currentMaterializationFingerprint)
    {
        if (!string.Equals(
                previousDiscoveryFingerprint,
                currentDiscoveryFingerprint,
                StringComparison.Ordinal))
            return WorldTowerBalancingReuseMode.RunDiscovery;
        return string.Equals(
                previousMaterializationFingerprint,
                currentMaterializationFingerprint,
                StringComparison.Ordinal)
            ? WorldTowerBalancingReuseMode.ReuseProfiles
            : WorldTowerBalancingReuseMode.RebuildProfiles;
    }
}

public sealed class WorldTowerAuditCampaignService : BackgroundService,
    IWorldTowerAuditCampaignService
{
    private const int SchemaVersion = 4;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WorldTowerAuditCampaignService> _logger;
    private readonly string _rootPath;
    private readonly JsonSerializerOptions _json;
    private readonly SemaphoreSlim _stateGate = new(1, 1);
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _running = new();
    private readonly Dictionary<Guid, WorldTowerAuditCampaign> _campaigns = [];
    private bool _loaded;

    public WorldTowerAuditCampaignService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<WorldTowerAuditCampaignService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localData))
            localData = Path.GetTempPath();
        _rootPath = Path.Combine(
            localData,
            "LegendsLegacy",
            "AdminDashboard",
            "combat-audit-campaigns");
        Directory.CreateDirectory(_rootPath);
        _json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        _json.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<IReadOnlyList<WorldTowerAuditCampaign>> ListAsync(
        CancellationToken cancellationToken)
    {
        await EnsureLoadedAsync(cancellationToken);
        await _stateGate.WaitAsync(cancellationToken);
        try
        {
            return _campaigns.Values
                .OrderByDescending(campaign => campaign.CreatedAtUtc)
                .ToArray();
        }
        finally
        {
            _stateGate.Release();
        }
    }

    public async Task<WorldTowerAuditCampaign?> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await EnsureLoadedAsync(cancellationToken);
        await _stateGate.WaitAsync(cancellationToken);
        try
        {
            return _campaigns.GetValueOrDefault(id);
        }
        finally
        {
            _stateGate.Release();
        }
    }

    public async Task<WorldTowerAuditCampaign> CreateAsync(
        WorldTowerAuditCampaignOptions options,
        CancellationToken cancellationToken)
    {
        options = Normalize(options);
        await EnsureLoadedAsync(cancellationToken);
        using var scope = _scopeFactory.CreateScope();
        var requirements = scope.ServiceProvider
            .GetRequiredService<WorldTowerProductionCalibrationRunner>()
            .GetProfileScenarioRequirements(options.MinimumFloor, options.MaximumFloor);
        if (requirements.Count == 0)
            throw new InvalidOperationException("No World Tower requirements matched the campaign range.");
        var expectedFloors = Enumerable.Range(
            options.MinimumFloor,
            options.MaximumFloor - options.MinimumFloor + 1);
        var coveredFloors = requirements.SelectMany(requirement => requirement.FloorNumbers)
            .Distinct()
            .Order()
            .ToArray();
        if (!expectedFloors.SequenceEqual(coveredFloors))
            throw new InvalidOperationException(
                $"The requested range is not fully authored. Covered floors: {string.Join(", ", coveredFloors)}.");
        var planned = WorldTowerAuditCampaignPlanner.Create(requirements, options);
        var fingerprints = await CreateFingerprintsAsync(
            scope.ServiceProvider,
            requirements,
            options,
            cancellationToken);
        var now = _timeProvider.GetUtcNow();
        var campaign = new WorldTowerAuditCampaign(
            SchemaVersion,
            Guid.NewGuid(),
            WorldTowerAuditCampaignStatus.Queued,
            options,
            now,
            now,
            planned.Scenarios,
            planned.Audits,
            DiscoveryFingerprint: fingerprints.Discovery,
            MaterializationFingerprint: fingerprints.Materialization);
        campaign = await ReuseCompatibleArtifactsAsync(campaign, cancellationToken);
        await SetCampaignAsync(campaign, cancellationToken);
        _queue.Writer.TryWrite(campaign.Id);
        return campaign;
    }

    public async Task<WorldTowerAuditCampaign?> CancelAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var current = await GetAsync(id, cancellationToken);
        if (current is null || IsTerminal(current.Status))
            return current;
        if (!_running.TryGetValue(id, out var source))
        {
            var cancelled = current with
            {
                Status = WorldTowerAuditCampaignStatus.Cancelled,
                CancelRequested = false,
                Audits = current.Audits.Select(audit =>
                    audit.Status == WorldTowerAuditWorkStatus.Queued
                        ? audit with { Status = WorldTowerAuditWorkStatus.Cancelled }
                        : audit).ToArray(),
                UpdatedAtUtc = _timeProvider.GetUtcNow()
            };
            await SetCampaignAsync(cancelled, cancellationToken);
            return cancelled;
        }

        var updated = current with
        {
            CancelRequested = true,
            UpdatedAtUtc = _timeProvider.GetUtcNow()
        };
        await SetCampaignAsync(updated, cancellationToken);
        await source.CancelAsync();
        return updated;
    }

    public async Task<WorldTowerAuditCampaign?> RetryAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var current = await GetAsync(id, cancellationToken);
        if (current is null)
            return null;
        if (current.SchemaVersion != SchemaVersion)
        {
            throw new InvalidOperationException(
                "This campaign uses a retired audit or evidence contract and cannot be retried. Create a new five-member party discovery campaign instead.");
        }
        if (current.Status is not (WorldTowerAuditCampaignStatus.Failed
            or WorldTowerAuditCampaignStatus.Cancelled))
            return current;
        using (var scope = _scopeFactory.CreateScope())
        {
            var fingerprints = await CreateFingerprintsAsync(
                scope.ServiceProvider,
                current.Scenarios.Select(scenario => scenario.Requirement).ToArray(),
                current.Options,
                cancellationToken);
            if (!string.Equals(
                    current.DiscoveryFingerprint,
                    fingerprints.Discovery,
                    StringComparison.Ordinal)
                || !string.Equals(
                    current.MaterializationFingerprint,
                    fingerprints.Materialization,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Combat or equipment inputs changed after this campaign started. Retry is unsafe; start a new one-click balancing run so dependency-aware reuse can be evaluated.");
            }
        }
        var resetAudits = current.Audits.Select(audit =>
            audit.Status == WorldTowerAuditWorkStatus.Completed
                ? audit
                : audit with
                {
                    Status = WorldTowerAuditWorkStatus.Queued,
                    StartedAtUtc = null,
                    CompletedAtUtc = null,
                    Error = null
                }).ToArray();
        var updated = current with
        {
            Status = WorldTowerAuditCampaignStatus.Queued,
            Audits = resetAudits,
            CancelRequested = false,
            CatalogIsValid = false,
            CatalogProfileSetCount = 0,
            CatalogIssueCount = 0,
            CatalogContentHash = null,
            CandidateSmokePassed = false,
            CandidateCertificationCompleted = false,
            CandidateCertificationPassed = false,
            CandidateCertificationIssueCount = 0,
            Error = null,
            UpdatedAtUtc = _timeProvider.GetUtcNow()
        };
        await SetCampaignAsync(updated, cancellationToken);
        _queue.Writer.TryWrite(id);
        return updated;
    }

    public async Task<CombatCharacterProfileCatalogDocument?> GetCatalogAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var campaign = await GetAsync(id, cancellationToken);
        if (campaign is null || !campaign.CatalogIsValid)
            return null;
        return await ReadArtifactAsync<CombatCharacterProfileCatalogDocument>(
            CatalogPath(id),
            cancellationToken);
    }

    public async Task<WorldTowerAuditCampaignEvidence?> GetEvidenceAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var campaign = await GetAsync(id, cancellationToken);
        if (campaign is null)
            return null;
        var reports = new Dictionary<string, AbilityBalanceAuditReport>(StringComparer.Ordinal);
        foreach (var audit in campaign.Audits.Where(audit =>
                     audit.Status == WorldTowerAuditWorkStatus.Completed))
        {
            var report = await ReadArtifactAsync<AbilityBalanceAuditReport>(
                AuditPath(id, audit.Id),
                cancellationToken);
            if (report is not null)
                reports[audit.Id] = report;
        }
        var validation = await ReadArtifactAsync<CombatCharacterProfileCatalogValidationReport>(
            CatalogValidationPath(id),
            cancellationToken);
        var smoke = await ReadArtifactAsync<WorldTowerProfileShadowCalibrationReport>(
            CandidateSmokePath(id),
            cancellationToken);
        var certification = await ReadArtifactAsync<WorldTowerCalibrationCertificationReport>(
            CandidateCertificationPath(id),
            cancellationToken);
        return new WorldTowerAuditCampaignEvidence(
            campaign,
            reports,
            validation,
            smoke,
            certification);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureLoadedAsync(stoppingToken);
        foreach (var campaign in await ListAsync(stoppingToken))
        {
            if (campaign.SchemaVersion != SchemaVersion
                && campaign.Status is WorldTowerAuditCampaignStatus.Queued
                    or WorldTowerAuditCampaignStatus.RunningAudits
                    or WorldTowerAuditCampaignStatus.GeneratingCatalog
                    or WorldTowerAuditCampaignStatus.RunningCandidateSmoke
                    or WorldTowerAuditCampaignStatus.RunningCandidateCertification)
            {
                var retired = campaign with
                {
                    Status = WorldTowerAuditCampaignStatus.Cancelled,
                    CancelRequested = false,
                    Audits = campaign.Audits.Select(audit =>
                        audit.Status is WorldTowerAuditWorkStatus.Queued
                            or WorldTowerAuditWorkStatus.Running
                            ? audit with
                            {
                                Status = WorldTowerAuditWorkStatus.Cancelled,
                                CompletedAtUtc = _timeProvider.GetUtcNow()
                            }
                            : audit).ToArray(),
                    Error = "This campaign uses a retired balancing contract. Start a new one-click run so current fingerprints and candidate verification are applied.",
                    UpdatedAtUtc = _timeProvider.GetUtcNow()
                };
                await SetCampaignAsync(retired, stoppingToken);
                continue;
            }
            if (campaign.Status is WorldTowerAuditCampaignStatus.Queued
                or WorldTowerAuditCampaignStatus.RunningAudits
                or WorldTowerAuditCampaignStatus.GeneratingCatalog
                or WorldTowerAuditCampaignStatus.RunningCandidateSmoke
                or WorldTowerAuditCampaignStatus.RunningCandidateCertification)
            {
                var resumed = campaign with
                {
                    Status = WorldTowerAuditCampaignStatus.Queued,
                    CancelRequested = false,
                    Audits = campaign.Audits.Select(audit =>
                        audit.Status == WorldTowerAuditWorkStatus.Running
                            ? audit with { Status = WorldTowerAuditWorkStatus.Queued }
                            : audit).ToArray(),
                    UpdatedAtUtc = _timeProvider.GetUtcNow()
                };
                await SetCampaignAsync(resumed, stoppingToken);
                _queue.Writer.TryWrite(resumed.Id);
            }
        }

        await foreach (var campaignId in _queue.Reader.ReadAllAsync(stoppingToken))
            await ProcessAsync(campaignId, stoppingToken);
    }

    private async Task ProcessAsync(Guid id, CancellationToken stoppingToken)
    {
        var campaign = await GetAsync(id, stoppingToken);
        if (campaign is null || campaign.Status != WorldTowerAuditCampaignStatus.Queued)
            return;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        if (!_running.TryAdd(id, cancellation))
            return;

        try
        {
            campaign = await GetAsync(id, cancellation.Token);
            if (campaign is null || campaign.Status != WorldTowerAuditCampaignStatus.Queued)
                return;
            await EnsureFingerprintsCurrentAsync(campaign, cancellation.Token);
            campaign = campaign with
            {
                Status = WorldTowerAuditCampaignStatus.RunningAudits,
                UpdatedAtUtc = _timeProvider.GetUtcNow()
            };
            await SetCampaignAsync(campaign, cancellation.Token);
            foreach (var audit in campaign.Audits.Where(audit =>
                         audit.Status != WorldTowerAuditWorkStatus.Completed))
            {
                cancellation.Token.ThrowIfCancellationRequested();
                campaign = (await GetAsync(id, cancellation.Token))!;
                await EnsureFingerprintsCurrentAsync(campaign, cancellation.Token);
                if (campaign.CancelRequested)
                    throw new OperationCanceledException(cancellation.Token);
                var runningAudit = campaign.Audits.Single(item => item.Id == audit.Id) with
                {
                    Status = WorldTowerAuditWorkStatus.Running,
                    AttemptCount = audit.AttemptCount + 1,
                    StartedAtUtc = _timeProvider.GetUtcNow(),
                    CompletedAtUtc = null,
                    Error = null
                };
                campaign = ReplaceAudit(campaign, runningAudit) with
                {
                    UpdatedAtUtc = _timeProvider.GetUtcNow()
                };
                await SetCampaignAsync(campaign, cancellation.Token);

                using var scope = _scopeFactory.CreateScope();
                var report = scope.ServiceProvider
                    .GetRequiredService<IAbilityBalanceAuditService>()
                    .Run(runningAudit.Request, cancellation.Token);
                await WriteArtifactAsync(AuditPath(id, audit.Id), report, cancellation.Token);
                var completedAudit = runningAudit with
                {
                    Status = WorldTowerAuditWorkStatus.Completed,
                    CompletedAtUtc = _timeProvider.GetUtcNow(),
                    TotalBattlesRun = report.TotalBattlesRun,
                    ContentHash = report.ContentHash
                };
                campaign = ReplaceAudit(campaign, completedAudit) with
                {
                    UpdatedAtUtc = _timeProvider.GetUtcNow()
                };
                await SetCampaignAsync(campaign, cancellation.Token);
            }

            campaign = (await GetAsync(id, cancellation.Token))!;
            await EnsureFingerprintsCurrentAsync(campaign, cancellation.Token);
            CombatCharacterProfileCatalogValidationReport catalogValidation;
            if (campaign.CatalogIsValid)
            {
                var reusedCatalog = await ReadArtifactAsync<CombatCharacterProfileCatalogDocument>(
                    CatalogPath(id),
                    cancellation.Token)
                    ?? throw new InvalidOperationException("The reusable candidate catalog artifact is missing.");
                using var validationScope = _scopeFactory.CreateScope();
                catalogValidation = await validationScope.ServiceProvider
                    .GetRequiredService<ICombatCharacterProfileCatalogService>()
                    .ValidateAsync(reusedCatalog, cancellation.Token);
            }
            else
            {
                campaign = campaign with
                {
                    Status = WorldTowerAuditCampaignStatus.GeneratingCatalog,
                    UpdatedAtUtc = _timeProvider.GetUtcNow()
                };
                await SetCampaignAsync(campaign, cancellation.Token);
                catalogValidation = await GenerateCatalogAsync(campaign, cancellation.Token);
            }
            await WriteArtifactAsync(
                CatalogValidationPath(id),
                catalogValidation,
                cancellation.Token);
            if (!catalogValidation.IsValid)
                throw new InvalidOperationException(
                    $"Generated catalog failed with {catalogValidation.Issues.Count} validation issues.");
            await WriteArtifactAsync(
                CatalogPath(id),
                catalogValidation.NormalizedCatalog,
                cancellation.Token);
            campaign = campaign with
            {
                CatalogIsValid = true,
                CatalogProfileSetCount = catalogValidation.NormalizedCatalog.ProfileSets.Count,
                CatalogIssueCount = catalogValidation.Issues.Count,
                CatalogContentHash = catalogValidation.CurrentContentHash,
                Error = null,
                UpdatedAtUtc = _timeProvider.GetUtcNow()
            };
            await SetCampaignAsync(campaign, cancellation.Token);

            if (campaign.Options.RunCandidateVerification)
            {
                campaign = campaign with
                {
                    Status = WorldTowerAuditCampaignStatus.RunningCandidateSmoke,
                    UpdatedAtUtc = _timeProvider.GetUtcNow()
                };
                await SetCampaignAsync(campaign, cancellation.Token);
                using (var smokeScope = _scopeFactory.CreateScope())
                {
                    var smoke = await smokeScope.ServiceProvider
                        .GetRequiredService<IWorldTowerProfileShadowCalibrationRunner>()
                        .RunCandidateAsync(
                            catalogValidation.NormalizedCatalog,
                            $"campaign:{campaign.Id:D}",
                            new WorldTowerProfileShadowCalibrationOptions(
                                campaign.Options.MinimumFloor,
                                campaign.Options.MaximumFloor,
                                campaign.Options.SmokeSampleCount,
                                RequireExpandedPortfolio: true,
                                WeightPolicy: null,
                                BaseRandomSeed: campaign.Options.ProfileRandomSeed,
                                SeedManifestId: $"world-tower-candidate-smoke:{campaign.Id:N}",
                                UseSharedCohortSeeds: true),
                            cancellation.Token);
                    await WriteArtifactAsync(CandidateSmokePath(id), smoke, cancellation.Token);
                    campaign = campaign with
                    {
                        CandidateSmokePassed = smoke.Status == WorldTowerProfileShadowCalibrationStatus.Completed,
                        UpdatedAtUtc = _timeProvider.GetUtcNow()
                    };
                    await SetCampaignAsync(campaign, cancellation.Token);
                }

                if (campaign.CandidateSmokePassed)
                {
                    campaign = campaign with
                    {
                        Status = WorldTowerAuditCampaignStatus.RunningCandidateCertification,
                        UpdatedAtUtc = _timeProvider.GetUtcNow()
                    };
                    await SetCampaignAsync(campaign, cancellation.Token);
                    using var certificationScope = _scopeFactory.CreateScope();
                    var certification = await certificationScope.ServiceProvider
                        .GetRequiredService<IWorldTowerCalibrationCertificationRunner>()
                        .RunCandidateAsync(
                            catalogValidation.NormalizedCatalog,
                            $"campaign:{campaign.Id:D}",
                            new WorldTowerCalibrationCertificationOptions(
                                campaign.Options.MinimumFloor,
                                campaign.Options.MaximumFloor,
                                campaign.Options.CertificationSampleCount,
                                campaign.Options.CertificationSampleCount,
                                BaseRandomSeed: campaign.Options.ProfileRandomSeed,
                                SeedManifestId: WorldTowerProfileTargetContract.CertificationSeedManifestId),
                            cancellation.Token);
                    await WriteArtifactAsync(
                        CandidateCertificationPath(id),
                        certification,
                        cancellation.Token);
                    campaign = campaign with
                    {
                        CandidateCertificationCompleted = true,
                        CandidateCertificationPassed = certification.IsCertified,
                        CandidateCertificationIssueCount = certification.Issues.Count,
                        UpdatedAtUtc = _timeProvider.GetUtcNow()
                    };
                    await SetCampaignAsync(campaign, cancellation.Token);
                }
            }

            campaign = campaign with
            {
                Status = WorldTowerAuditCampaignStatus.Completed,
                Error = null,
                UpdatedAtUtc = _timeProvider.GetUtcNow()
            };
            await SetCampaignAsync(campaign, cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            var current = await GetAsync(id, CancellationToken.None);
            if (current is not null)
            {
                var stopping = stoppingToken.IsCancellationRequested;
                var status = stopping
                    ? WorldTowerAuditCampaignStatus.Queued
                    : WorldTowerAuditCampaignStatus.Cancelled;
                var workStatus = stopping
                    ? WorldTowerAuditWorkStatus.Queued
                    : WorldTowerAuditWorkStatus.Cancelled;
                current = current with
                {
                    Status = status,
                    CancelRequested = false,
                    Audits = current.Audits.Select(audit =>
                        audit.Status == WorldTowerAuditWorkStatus.Running
                            ? audit with { Status = workStatus, Error = stopping ? null : "Cancelled." }
                            : audit).ToArray(),
                    UpdatedAtUtc = _timeProvider.GetUtcNow()
                };
                await SetCampaignAsync(current, CancellationToken.None);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "World Tower audit campaign {CampaignId} failed.", id);
            var current = await GetAsync(id, CancellationToken.None);
            if (current is not null)
            {
                current = current with
                {
                    Status = WorldTowerAuditCampaignStatus.Failed,
                    Error = exception.Message,
                    Audits = current.Audits.Select(audit =>
                        audit.Status == WorldTowerAuditWorkStatus.Running
                            ? audit with
                            {
                                Status = WorldTowerAuditWorkStatus.Failed,
                                CompletedAtUtc = _timeProvider.GetUtcNow(),
                                Error = exception.Message
                            }
                            : audit).ToArray(),
                    UpdatedAtUtc = _timeProvider.GetUtcNow()
                };
                await SetCampaignAsync(current, CancellationToken.None);
            }
        }
        finally
        {
            _running.TryRemove(id, out _);
        }
    }

    private async Task<CombatCharacterProfileCatalogValidationReport> GenerateCatalogAsync(
        WorldTowerAuditCampaign campaign,
        CancellationToken cancellationToken)
    {
        var reports = new Dictionary<string, AbilityBalanceAuditReport>(StringComparer.Ordinal);
        foreach (var audit in campaign.Audits)
        {
            reports[audit.Id] = await ReadArtifactAsync<AbilityBalanceAuditReport>(
                    AuditPath(campaign.Id, audit.Id),
                    cancellationToken)
                ?? throw new InvalidOperationException($"Audit artifact '{audit.Id}' is missing.");
        }
        var requests = campaign.Scenarios.Select(scenario =>
        {
            var audit = campaign.Audits.Single(item => item.Id == scenario.AuditWorkId);
            var requirement = scenario.Requirement;
            return new CombatCharacterProfileGenerationRequest(
                $"{campaign.Id:N}:{audit.Id}:{requirement.ScenarioId}",
                reports[audit.Id],
                "WorldTower",
                requirement.EquipmentQuality,
                campaign.Options.TeamsPerFamily,
                campaign.Options.ProfileRandomSeed,
                "Expanded",
                campaign.Options.MinimumSourceBattles,
                campaign.Options.MinimumMatchupBattles,
                campaign.Options.MaximumConfidenceWidth95,
                campaign.Options.MaximumSeedScoreSpread,
                campaign.Options.MaximumEssenceOverlap,
                campaign.Options.RequireMultiSeedStability,
                requirement.TeamSize,
                requirement.EquipmentTier,
                requirement.EquipmentRarity,
                requirement.FloorNumbers,
                ContextQualificationSampleCount: 10);
        }).ToArray();
        using var scope = _scopeFactory.CreateScope();
        var batch = await scope.ServiceProvider
            .GetRequiredService<ICombatCharacterProfileBatchService>()
            .GenerateCatalogAsync(new(requests), cancellationToken);
        return batch.CatalogValidation;
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
            return;
        await _stateGate.WaitAsync(cancellationToken);
        try
        {
            if (_loaded)
                return;
            foreach (var path in Directory.EnumerateFiles(
                         _rootPath,
                         "campaign.json",
                         SearchOption.AllDirectories))
            {
                try
                {
                    var campaign = await ReadArtifactAsync<WorldTowerAuditCampaign>(
                        path,
                        cancellationToken);
                    if (campaign is not null)
                        _campaigns[campaign.Id] = campaign;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Could not load audit campaign state from {Path}.", path);
                }
            }
            _loaded = true;
        }
        finally
        {
            _stateGate.Release();
        }
    }

    private async Task SetCampaignAsync(
        WorldTowerAuditCampaign campaign,
        CancellationToken cancellationToken)
    {
        await _stateGate.WaitAsync(cancellationToken);
        try
        {
            _campaigns[campaign.Id] = campaign;
            await WriteArtifactAsync(CampaignPath(campaign.Id), campaign, cancellationToken);
        }
        finally
        {
            _stateGate.Release();
        }
    }

    private async Task WriteArtifactAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        await using (var stream = File.Create(temporaryPath))
            await JsonSerializer.SerializeAsync(stream, value, _json, cancellationToken);
        File.Move(temporaryPath, path, overwrite: true);
    }

    private async Task<T?> ReadArtifactAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return default;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, _json, cancellationToken);
    }

    private static WorldTowerAuditCampaign ReplaceAudit(
        WorldTowerAuditCampaign campaign,
        WorldTowerAuditCampaignWork replacement) => campaign with
    {
        Audits = campaign.Audits.Select(audit =>
            audit.Id == replacement.Id ? replacement : audit).ToArray()
    };

    private string CampaignDirectory(Guid id) => Path.Combine(_rootPath, id.ToString("N"));
    private string CampaignPath(Guid id) => Path.Combine(CampaignDirectory(id), "campaign.json");
    private string AuditPath(Guid id, string auditId) =>
        Path.Combine(CampaignDirectory(id), "audits", $"{auditId}.json");
    private string CatalogPath(Guid id) => Path.Combine(CampaignDirectory(id), "catalog.json");
    private string CatalogValidationPath(Guid id) =>
        Path.Combine(CampaignDirectory(id), "catalog-validation.json");
    private string CandidateSmokePath(Guid id) =>
        Path.Combine(CampaignDirectory(id), "candidate-smoke.json");
    private string CandidateCertificationPath(Guid id) =>
        Path.Combine(CampaignDirectory(id), "candidate-certification.json");

    private static bool IsTerminal(WorldTowerAuditCampaignStatus status) => status is
        WorldTowerAuditCampaignStatus.Completed
        or WorldTowerAuditCampaignStatus.Failed
        or WorldTowerAuditCampaignStatus.Cancelled;

    private static WorldTowerAuditCampaignOptions Normalize(
        WorldTowerAuditCampaignOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.MinimumFloor is < 1 or > 20
            || options.MaximumFloor < options.MinimumFloor
            || options.MaximumFloor > 20)
            throw new ArgumentOutOfRangeException(nameof(options));
        var seeds = (options.RandomSeeds ?? [1337, 2027, 9001])
            .Where(seed => seed != 0)
            .Distinct()
            .Take(10)
            .ToArray();
        if (seeds.Length == 0)
            seeds = [1337, 2027, 9001];
        return options with
        {
            CandidatePoolSize = Math.Clamp(options.CandidatePoolSize, 2, 1_000),
            ScreeningBattleCount = Math.Max(1, options.ScreeningBattleCount),
            FinalistCount = Math.Clamp(options.FinalistCount, 2, 1_000),
            FinalistBattleCount = Math.Max(1, options.FinalistBattleCount),
            ValidationBattleCount = Math.Max(1, options.ValidationBattleCount),
            RandomSeeds = seeds,
            TeamsPerFamily = Math.Clamp(options.TeamsPerFamily, 1, 10),
            ProfileRandomSeed = options.ProfileRandomSeed == 0 ? 1337 : options.ProfileRandomSeed,
            MinimumSourceBattles = Math.Max(1, options.MinimumSourceBattles),
            MinimumMatchupBattles = Math.Max(1, options.MinimumMatchupBattles),
            MaximumConfidenceWidth95 = UnitInterval(
                options.MaximumConfidenceWidth95,
                nameof(options.MaximumConfidenceWidth95)),
            MaximumSeedScoreSpread = UnitInterval(
                options.MaximumSeedScoreSpread,
                nameof(options.MaximumSeedScoreSpread)),
            MaximumEssenceOverlap = UnitInterval(
                options.MaximumEssenceOverlap,
                nameof(options.MaximumEssenceOverlap)),
            SmokeSampleCount = Math.Clamp(options.SmokeSampleCount, 1, 100),
            CertificationSampleCount = Math.Clamp(options.CertificationSampleCount, 10, 1_000),
            DiscoveryEquipmentTier = Math.Clamp(options.DiscoveryEquipmentTier, 1, 10),
            DiscoveryEquipmentRarity = string.IsNullOrWhiteSpace(options.DiscoveryEquipmentRarity)
                ? "Epic"
                : options.DiscoveryEquipmentRarity.Trim(),
            DiscoveryEquipmentProfile = string.IsNullOrWhiteSpace(options.DiscoveryEquipmentProfile)
                ? "Balanced"
                : options.DiscoveryEquipmentProfile.Trim()
        };
    }

    private static double UnitInterval(double value, string name)
    {
        if (!double.IsFinite(value) || value is < 0d or > 1d)
            throw new ArgumentOutOfRangeException(name);
        return value;
    }

    private static async Task<(string Discovery, string Materialization)> CreateFingerprintsAsync(
        IServiceProvider serviceProvider,
        IReadOnlyList<WorldTowerProfileScenarioRequirement> requirements,
        WorldTowerAuditCampaignOptions options,
        CancellationToken cancellationToken)
    {
        var abilities = serviceProvider.GetRequiredService<IAbilityCatalogProvider>();
        var essences = serviceProvider.GetRequiredService<IEssenceDefinitionRepository>();
        var builds = serviceProvider.GetRequiredService<CanonicalEquipmentBuildFactory>();
        var floorNumbers = requirements
            .SelectMany(requirement => requirement.FloorNumbers)
            .Distinct()
            .Order()
            .ToArray();
        var floors = serviceProvider.GetRequiredService<IWorldTowerDefinitionProvider>()
            .GetFloors()
            .Where(floor => floorNumbers.Contains(floor.FloorNumber))
            .OrderBy(floor => floor.FloorNumber)
            .ToArray();
        if (floors.Length != floorNumbers.Length)
            throw new InvalidOperationException("Fingerprinting could not resolve every requested Tower floor.");
        var guardianIds = floors.Select(floor => floor.GuardianCreatureId).Distinct().ToArray();
        var guardians = (await serviceProvider.GetRequiredService<IEntityService>()
                .GetEntitiesByIdsForCombatAsync(guardianIds.ToList(), cancellationToken))
            .OfType<Creature>()
            .OrderBy(guardian => guardian.Id)
            .ToArray();
        if (guardians.Length != guardianIds.Length)
            throw new InvalidOperationException("Fingerprinting could not resolve every requested Tower guardian.");
        return (
            AbilityBalanceContentFingerprint.CreateDiscovery(
                abilities,
                essences,
                builds,
                options.DiscoveryEquipmentTier,
                options.DiscoveryEquipmentRarity,
                options.DiscoveryEquipmentProfile),
            AbilityBalanceContentFingerprint.CreateMaterialization(
                abilities,
                essences,
                builds,
                requirements,
                floors,
                guardians,
                serviceProvider.GetRequiredService<ICreatureAbilityDefinitionProvider>(),
                serviceProvider.GetRequiredService<ICreatureEssenceLootTableRepository>(),
                serviceProvider.GetRequiredService<IRegionCreatureScalingProvider>()));
    }

    private async Task EnsureFingerprintsCurrentAsync(
        WorldTowerAuditCampaign campaign,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var current = await CreateFingerprintsAsync(
            scope.ServiceProvider,
            campaign.Scenarios.Select(scenario => scenario.Requirement).ToArray(),
            campaign.Options,
            cancellationToken);
        if (!string.Equals(campaign.DiscoveryFingerprint, current.Discovery, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Essence, ability, combat-rule, or discovery-equipment inputs changed while the balancing run was in progress. The run was stopped before mixed evidence could be produced; start a new one-click run.");
        }
        if (!string.Equals(
                campaign.MaterializationFingerprint,
                current.Materialization,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Target equipment or profile-materialization inputs changed while the balancing run was in progress. The run was stopped before mixed evidence could be produced; start a new one-click run.");
        }
    }

    private async Task<WorldTowerAuditCampaign> ReuseCompatibleArtifactsAsync(
        WorldTowerAuditCampaign campaign,
        CancellationToken cancellationToken)
    {
        var candidates = (await ListAsync(cancellationToken))
            .Where(candidate => candidate.SchemaVersion == SchemaVersion)
            .Where(candidate => WorldTowerBalancingDependencyPlanner.Decide(
                candidate.DiscoveryFingerprint,
                candidate.MaterializationFingerprint,
                campaign.DiscoveryFingerprint!,
                campaign.MaterializationFingerprint!)
                != WorldTowerBalancingReuseMode.RunDiscovery)
            .OrderByDescending(candidate => candidate.CreatedAtUtc)
            .ToArray();
        if (candidates.Length == 0)
            return campaign;

        using var scope = _scopeFactory.CreateScope();
        var currentContentHash = AbilityBalanceContentFingerprint.Create(
            scope.ServiceProvider.GetRequiredService<IAbilityCatalogProvider>(),
            scope.ServiceProvider.GetRequiredService<IEssenceDefinitionRepository>());
        var audits = campaign.Audits.ToArray();
        var reusedCount = 0;
        for (var index = 0; index < audits.Length; index++)
        {
            var target = audits[index];
            var source = candidates
                .Select(candidate => new
                {
                    Campaign = candidate,
                    Audit = candidate.Audits.FirstOrDefault(audit =>
                        audit.Status == WorldTowerAuditWorkStatus.Completed
                        && string.Equals(
                            AuditRequestSignature(audit.Request),
                            AuditRequestSignature(target.Request),
                            StringComparison.Ordinal))
                })
                .FirstOrDefault(candidate => candidate.Audit is not null);
            if (source?.Audit is null)
                continue;
            var report = await ReadArtifactAsync<AbilityBalanceAuditReport>(
                AuditPath(source.Campaign.Id, source.Audit.Id),
                cancellationToken);
            if (report is null)
                continue;
            var normalizedReport = report with { ContentHash = currentContentHash };
            await WriteArtifactAsync(
                AuditPath(campaign.Id, target.Id),
                normalizedReport,
                cancellationToken);
            audits[index] = target with
            {
                Status = WorldTowerAuditWorkStatus.Completed,
                CompletedAtUtc = _timeProvider.GetUtcNow(),
                TotalBattlesRun = report.TotalBattlesRun,
                ContentHash = currentContentHash,
                ReusedFromCampaignId = source.Campaign.Id,
                ReusedSourceContentHash = report.ContentHash
            };
            reusedCount++;
        }

        campaign = campaign with
        {
            Audits = audits,
            ReusedAuditCount = reusedCount
        };
        if (reusedCount != audits.Length)
            return campaign;

        var catalogSource = candidates.FirstOrDefault(candidate =>
            candidate.CatalogIsValid
            && WorldTowerBalancingDependencyPlanner.Decide(
                candidate.DiscoveryFingerprint,
                candidate.MaterializationFingerprint,
                campaign.DiscoveryFingerprint!,
                campaign.MaterializationFingerprint!)
                == WorldTowerBalancingReuseMode.ReuseProfiles);
        if (catalogSource is null)
            return campaign;
        var catalog = await ReadArtifactAsync<CombatCharacterProfileCatalogDocument>(
            CatalogPath(catalogSource.Id),
            cancellationToken);
        if (catalog is null)
            return campaign;
        await WriteArtifactAsync(CatalogPath(campaign.Id), catalog, cancellationToken);
        return campaign with
        {
            CatalogIsValid = true,
            CatalogProfileSetCount = catalog.ProfileSets.Count,
            CatalogContentHash = catalogSource.CatalogContentHash,
            ReusedCatalogFromCampaignId = catalogSource.Id
        };
    }

    private string AuditRequestSignature(AbilityBalanceAuditRequest request) =>
        JsonSerializer.Serialize(request, _json);
}
