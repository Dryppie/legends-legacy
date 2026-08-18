using System.Text.Json;
using Application.Interfaces.Services.LL.Administration;
using Application.UseCases.Administration;
using Domain.Models.Administration;
using Microsoft.Extensions.Options;

namespace Services.LL.Administration;

public sealed class LiveOpsAccountRiskService(
    IAccountRiskRepository riskRepository,
    IAdministrationRepository administrationRepository,
    IOptions<AccountRiskOptions> configuredOptions,
    TimeProvider timeProvider) : ILiveOpsAccountRiskService
{
    private readonly AccountRiskOptions _options = configuredOptions.Value;

    public async Task<int> RefreshAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled) return 0;
        await riskRepository.AcquireEvaluationLockAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var lastEvaluation = await riskRepository.GetLastEvaluatedAtAsync(cancellationToken);
        var freshnessWindow = TimeSpan.FromMinutes(Math.Max(1, _options.EvaluationIntervalMinutes) * 0.8);
        if (lastEvaluation >= now - freshnessWindow) return 0;
        var since = now.AddDays(-_options.LookbackDays);
        var candidates = await riskRepository.GetCandidateAccountIdsAsync(
            since,
            _options.CandidateLimit,
            cancellationToken);
        if (candidates.Count == 0) return 0;

        var dataset = await riskRepository.GetAnalysisDatasetAsync(
            candidates,
            since,
            _options.MaximumTransfersPerEvaluation,
            cancellationToken);
        var evaluator = new AccountRiskEvaluator(_options.ToPolicy());
        var evaluations = candidates
            .Where(dataset.Accounts.ContainsKey)
            .Select(accountId => evaluator.Evaluate(accountId, dataset, now))
            .ToList();
        await riskRepository.UpsertEvaluationsAsync(
            evaluations,
            now,
            _options.HistoryMinimumScoreChange,
            cancellationToken);
        return evaluations.Count;
    }

    public Task<AccountRiskPage> SearchAsync(AccountRiskSearch search, CancellationToken cancellationToken) =>
        riskRepository.SearchAsync(search, cancellationToken);

    public Task<AccountRiskDetails?> GetDetailsAsync(Guid accountId, int transferLimit, CancellationToken cancellationToken) =>
        riskRepository.GetDetailsAsync(accountId, transferLimit, cancellationToken);

    public async Task<AccountRiskOperationResult> UpdateStatusAsync(
        Guid operationId,
        Guid accountId,
        AccountInvestigationStatus status,
        AdministrationActor actor,
        string reason,
        CancellationToken cancellationToken)
    {
        var validation = Validate(operationId, actor, reason, 500);
        if (validation is not null) return AccountRiskOperationResult.Fail(validation);
        var existingAction = await administrationRepository.GetActionAsync(operationId, cancellationToken);
        if (existingAction is not null)
        {
            var previousRequest = TryReadStatusRequest(existingAction.DetailsJson);
            return existingAction.ActionType == AdminActionType.AccountRiskStatusChanged &&
                   existingAction.TargetAccountId == accountId &&
                   previousRequest?.Status == status &&
                   string.Equals(existingAction.Reason, reason.Trim(), StringComparison.Ordinal)
                ? AccountRiskOperationResult.Success(operationId, true, previousRequest.Status)
                : AccountRiskOperationResult.Fail("That operation ID was already used with different action parameters.");
        }
        var snapshot = await riskRepository.GetSnapshotAsync(accountId, cancellationToken);
        if (snapshot is null) return AccountRiskOperationResult.Fail("The account-risk snapshot was not found.");
        var now = timeProvider.GetUtcNow();
        var investigation = await riskRepository.GetInvestigationAsync(accountId, cancellationToken);
        var previous = investigation?.Status ?? AccountInvestigationStatus.Unreviewed;
        if (investigation is null)
        {
            investigation = new AccountRiskInvestigation { AccountId = accountId };
            riskRepository.AddInvestigation(investigation);
        }
        investigation.Status = status;
        investigation.UpdatedBySubject = actor.Subject.Trim();
        investigation.UpdatedAt = now;
        riskRepository.AddAdminAction(new AdminAction
        {
            Id = operationId,
            ActionType = AdminActionType.AccountRiskStatusChanged,
            Permission = AdministrationPermissions.AccountModeration,
            ActorSubject = actor.Subject.Trim(),
            ActorDisplayName = actor.DisplayName.Trim(),
            TargetAccountId = accountId,
            TargetCharacterId = snapshot.CharacterId,
            Reason = reason.Trim(),
            DetailsJson = JsonSerializer.Serialize(new { PreviousStatus = previous, Status = status, AutomatedRisk = snapshot.Severity, snapshot.Score }),
            RiskLevel = AdministrationRiskLevel.Normal,
            OccurredAt = now
        });
        return AccountRiskOperationResult.Success(operationId, false, status);
    }

    public async Task<AccountRiskOperationResult> AddNoteAsync(
        Guid operationId,
        Guid accountId,
        AdministrationActor actor,
        string note,
        CancellationToken cancellationToken)
    {
        var validation = Validate(operationId, actor, note, 4_000);
        if (validation is not null) return AccountRiskOperationResult.Fail(validation);
        var existingAction = await administrationRepository.GetActionAsync(operationId, cancellationToken);
        if (existingAction is not null)
        {
            return existingAction.ActionType == AdminActionType.AccountRiskNoteAdded &&
                   existingAction.TargetAccountId == accountId &&
                   string.Equals(existingAction.InternalNotes, note.Trim(), StringComparison.Ordinal)
                ? AccountRiskOperationResult.Success(operationId, true)
                : AccountRiskOperationResult.Fail("That operation ID was already used with different action parameters.");
        }
        var snapshot = await riskRepository.GetSnapshotAsync(accountId, cancellationToken);
        if (snapshot is null) return AccountRiskOperationResult.Fail("The account-risk snapshot was not found.");
        var now = timeProvider.GetUtcNow();
        var riskNote = new AccountRiskNote
        {
            AccountId = accountId,
            ActorSubject = actor.Subject.Trim(),
            ActorDisplayName = actor.DisplayName.Trim(),
            Body = note.Trim(),
            CreatedAt = now
        };
        riskRepository.AddNote(riskNote);
        riskRepository.AddAdminAction(new AdminAction
        {
            Id = operationId,
            ActionType = AdminActionType.AccountRiskNoteAdded,
            Permission = AdministrationPermissions.AccountModeration,
            ActorSubject = actor.Subject.Trim(),
            ActorDisplayName = actor.DisplayName.Trim(),
            TargetAccountId = accountId,
            TargetCharacterId = snapshot.CharacterId,
            TargetResourceId = riskNote.Id,
            Reason = "Investigation note added.",
            InternalNotes = riskNote.Body,
            DetailsJson = JsonSerializer.Serialize(new { AutomatedRisk = snapshot.Severity, snapshot.Score }),
            RiskLevel = AdministrationRiskLevel.Normal,
            OccurredAt = now
        });
        return AccountRiskOperationResult.Success(operationId, false, note: riskNote);
    }

    private static string? Validate(Guid operationId, AdministrationActor actor, string value, int maximumLength)
    {
        if (operationId == Guid.Empty) return "A non-empty operation ID is required.";
        if (string.IsNullOrWhiteSpace(actor.Subject)) return "The operator identity is missing.";
        if (string.IsNullOrWhiteSpace(value)) return "A reason or note is required.";
        return value.Trim().Length > maximumLength ? $"The value cannot exceed {maximumLength:N0} characters." : null;
    }

    private static StatusAuditDetails? TryReadStatusRequest(string detailsJson)
    {
        try { return JsonSerializer.Deserialize<StatusAuditDetails>(detailsJson); }
        catch (JsonException) { return null; }
    }

    private sealed record StatusAuditDetails(
        AccountInvestigationStatus PreviousStatus,
        AccountInvestigationStatus Status);
}
