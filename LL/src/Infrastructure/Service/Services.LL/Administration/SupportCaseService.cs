using System.Security.Cryptography;
using System.Text.Json;
using Application.Interfaces.Services.LL.Administration;
using Application.UseCases.Administration;
using Common.Primitives;
using Domain.Models.Administration;

namespace Services.LL.Administration;

public sealed class SupportCaseService(ISupportCaseRepository repository, IAdministrationRepository administration,
    IChatModerationGateway chat, TimeProvider time) : ISupportCaseService
{
    private static readonly HashSet<string> Categories = ["Missing item", "Activity", "Restriction", "Chat", "Transfer", "Signets", "Other"];
    public Task<SupportCasePage> SearchAsync(Guid? characterId, SupportCaseStatus? status, string? search, int page, CancellationToken ct) =>
        repository.SearchAsync(characterId, status, search?.Trim()[..Math.Min(search.Trim().Length, 160)], Math.Clamp(page, 1, 10000), ct);

    public async Task<Response<SupportCaseDetails>> GetAsync(Guid id, int? beforeSequence, CancellationToken ct)
    {
        var value = await repository.GetAsync(id, ct);
        if (value is null) return Response<SupportCaseDetails>.Fail("Case not found.", "case_not_found");
        var entries = await repository.EntriesAsync(id, beforeSequence, ct);
        var oldest = entries.Count > 0 ? entries.Min(x => x.Sequence) : 1;
        return Response<SupportCaseDetails>.Success(new(value, entries, oldest > 1 ? oldest : null));
    }

    public async Task<Response<SupportCaseDetails>> ApplyAsync(SupportCaseChange input, AdministrationActor actor, CancellationToken ct)
    {
        var c = input with { Body = input.Body?.Trim() ?? "", Title = Clean(input.Title),
            Category = Clean(input.Category), ExternalReference = Clean(input.ExternalReference), EvidenceReference = Clean(input.EvidenceReference) };
        var caseId = c.Kind == SupportCaseEntryKind.Created ? c.OperationId : c.CaseId;
        if (c.OperationId == Guid.Empty || caseId == Guid.Empty || c.Body.Length is < 1 or > 4000 ||
            c.EvidenceReference?.Length > 500 || c.ExternalReference?.Length > 300 ||
            string.IsNullOrWhiteSpace(actor.Subject) || actor.Subject.Length > 320 || actor.DisplayName.Length > 320)
            return Response<SupportCaseDetails>.Fail("Supply an operation reference and 1–4000 characters of case context. References must be within their displayed limits.");
        if (c.Kind == SupportCaseEntryKind.Created && (c.CharacterId == Guid.Empty || c.Title?.Length is not (>= 3 and <= 160) ||
            c.Category is null || !Categories.Contains(c.Category)))
            return Response<SupportCaseDetails>.Fail("Select a player, category and title (3–160 characters).");
        if (c.Kind == SupportCaseEntryKind.StatusChanged && (!c.Status.HasValue || !Enum.IsDefined(c.Status.Value)))
            return Response<SupportCaseDetails>.Fail("Select a valid case status.");
        var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(c)));
        await repository.LockAsync(c.OperationId, caseId, ct);
        var previous = await repository.GetOperationAsync(c.OperationId, ct);
        if (previous is not null)
            return previous.CaseId == caseId && previous.ActorSubject == actor.Subject && previous.RequestHash == hash
                ? await GetAsync(caseId, null, ct)
                : Response<SupportCaseDetails>.Conflict("This operation reference belongs to another request.", "case_operation_conflict");
        if (await administration.GetActionAsync(c.OperationId, ct) is not null)
            return Response<SupportCaseDetails>.Conflict("This operation reference is already in use.", "case_operation_conflict");
        SupportCase? value;
        var now = time.GetUtcNow();
        if (c.Kind == SupportCaseEntryKind.Created)
        {
            var player = await administration.GetPlayerByCharacterIdAsync(c.CharacterId, now, ct);
            if (player is null) return Response<SupportCaseDetails>.Fail("The player could not be found.");
            value = new SupportCase { Id = caseId, AccountId = player.AccountId, CharacterId = player.CharacterId,
                CharacterName = player.CharacterName, Title = c.Title!, Category = c.Category!,
                ExternalReference = c.ExternalReference, CreatedAt = now, Status = SupportCaseStatus.Open };
        }
        else
        {
            value = await repository.GetAsync(caseId, ct);
            if (value is null) return Response<SupportCaseDetails>.Fail("Case not found.", "case_not_found");
            if (value.Version != c.ExpectedVersion)
                return Response<SupportCaseDetails>.Conflict("This case changed. Refresh the case and review your draft before resubmitting.", "case_version_conflict");
            if (value.Status == SupportCaseStatus.Closed && c.Kind != SupportCaseEntryKind.StatusChanged)
                return Response<SupportCaseDetails>.Fail("Reopen the case before adding evidence or operations.");
        }
        if (c.Kind == SupportCaseEntryKind.OperationLinked)
        {
            if (c.LinkedOperationId is null || c.LinkedOperationId == Guid.Empty || c.LinkedSource is not ("Game" or "Chat"))
                return Response<SupportCaseDetails>.Fail("Choose a completed Game or Chat operation.");
            if (c.LinkedSource == "Game")
            {
                var operation = await administration.GetActionAsync(c.LinkedOperationId.Value, ct);
                if (operation is null || (operation.TargetCharacterId != value.CharacterId && operation.TargetAccountId != value.AccountId))
                    return Response<SupportCaseDetails>.Fail("The operation was not found for this player or account.");
            }
            else
            {
                var result = await chat.GetAuditAsync(new(null, null, null, null, null, c.LinkedOperationId,
                    [value.CharacterId], null, null, null, 1), ct);
                if (!result.IsSuccess) return Response<SupportCaseDetails>.Fail("Chat evidence is unavailable. Retry when Chat recovers.");
                if (!result.Entries.Any(x => x.OperationId == c.LinkedOperationId && x.CharacterId == value.CharacterId))
                    return Response<SupportCaseDetails>.Fail("The Chat operation was not found for this player.");
            }
        }
        // All checks precede tracked changes: the shared transaction pipeline also saves failed responses.
        if (c.Kind == SupportCaseEntryKind.Created) repository.Add(value);
        if (c.Kind == SupportCaseEntryKind.StatusChanged)
        {
            value.Status = c.Status!.Value;
            value.Resolution = value.Status is SupportCaseStatus.Resolved or SupportCaseStatus.Closed ? c.Body : null;
        }
        value.Version++; value.UpdatedAt = now;
        var entry = new SupportCaseEntry { Id = c.OperationId, CaseId = caseId, Sequence = value.Version,
            Kind = c.Kind, ActorSubject = actor.Subject, ActorDisplayName = actor.DisplayName, Body = c.Body,
            EvidenceReference = c.EvidenceReference, LinkedOperationId = c.LinkedOperationId,
            LinkedSource = c.LinkedSource, RequestHash = hash, CreatedAt = now };
        repository.Append(entry);
        var actionType = c.Kind switch { SupportCaseEntryKind.Created => AdminActionType.SupportCaseCreated,
            SupportCaseEntryKind.Note => AdminActionType.SupportCaseNoteAdded,
            SupportCaseEntryKind.OperationLinked => AdminActionType.SupportCaseOperationLinked,
            _ => AdminActionType.SupportCaseUpdated };
        administration.AddAction(new AdminAction { Id = c.OperationId, ActionType = actionType,
            Permission = AdministrationPermissions.AccountModeration, ActorSubject = actor.Subject,
            ActorDisplayName = actor.DisplayName, TargetAccountId = value.AccountId, TargetCharacterId = value.CharacterId,
            TargetResourceId = caseId, Reason = $"Case {caseId:D}: {c.Kind}", InternalNotes = c.Body,
            DetailsJson = JsonSerializer.Serialize(new { CaseId = caseId, Status = value.Status.ToString(), value.Version, c.LinkedOperationId, c.LinkedSource }), OccurredAt = now });
        var history = await repository.EntriesAsync(caseId, null, ct);
        var entries = history.Prepend(entry).DistinctBy(x => x.Id).Take(50).ToArray();
        return Response<SupportCaseDetails>.Success(new(value, entries, entries.Min(x => x.Sequence) > 1 ? entries.Min(x => x.Sequence) : null));
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
