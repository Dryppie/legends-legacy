using System.Text.Json;
using Application.Interfaces.Services.LL.Administration;
using Common.Primitives;
using Domain.Models.Administration;

namespace Services.LL.Administration;

public sealed class OperatorDraftService(IOperatorDraftRepository repository, TimeProvider time) : IOperatorDraftService
{
    private static readonly Dictionary<string, int> CaseFields = new()
    {
        ["title"] = 160, ["category"] = 40, ["description"] = 4000, ["externalReference"] = 300,
        ["note"] = 4000, ["evidence"] = 500, ["resolution"] = 4000, ["nextStatus"] = 32,
        ["playerResponse"] = 4000, ["priority"] = 16, ["followUpAt"] = 40, ["nextAction"] = 500, ["linkedOperation"] = 36, ["linkedSource"] = 16, ["linkReason"] = 4000
    };
    private static readonly Dictionary<string, int> WorkspaceFields = new()
    {
        ["analyticsPreferences"] = 6000, ["casePage"] = 8, ["caseCategory"] = 40, ["caseSort"] = 24, ["caseOverdue"] = 5, ["playerQuery"] = 160, ["caseSearch"] = 160, ["caseStatus"] = 32,
        ["auditSource"] = 16, ["auditActionType"] = 80, ["auditActor"] = 320,
        ["auditPermission"] = 80, ["auditReference"] = 500, ["auditRiskLevel"] = 24,
        ["auditTarget"] = 320, ["auditOperationId"] = 36, ["auditFrom"] = 40, ["auditTo"] = 40
    };
    private static readonly Dictionary<string, int> PlayerFields = new() { ["usePackageId"] = 40, ["compensationMode"] = 16, ["banReason"] = 2200, ["banNotes"] = 4200, ["banDuration"] = 1000, ["unbanReason"] = 2200, ["multiplayerRestrictionReason"] = 2200, ["multiplayerRestrictionNotes"] = 4200, ["multiplayerRestrictionDuration"] = 1000, ["multiplayerRestrictionRevokeReason"] = 2200, ["muteReason"] = 2200, ["muteDuration"] = 1000, ["unmuteReason"] = 2200, ["itemQuery"] = 1000, ["selectedItem"] = 8000, ["grantQuantity"] = 1000, ["equipmentDefinitionId"] = 1000, ["equipmentTier"] = 1000, ["equipmentRank"] = 1000, ["equipmentStyleId"] = 1000, ["grantReason"] = 2200, ["grantNotes"] = 4200, ["signetReason"] = 2200, ["signetQuantity"] = 1000, ["packageId"] = 1000, ["packageVersion"] = 1000, ["packageName"] = 1000, ["packagePurpose"] = 2200, ["packageArchived"] = 1000, ["packageLines"] = 8000 };
    private static readonly Dictionary<string, int> InvestigationFields = new() { ["note"] = 8200, ["statusReason"] = 1200, ["selectedStatus"] = 64 };
    private static readonly Dictionary<string, int> RecoveryFields = new() { ["reason"] = 2200 };
    public async Task<Response<OperatorDraft>> GetAsync(string actor, string key, CancellationToken ct)
    {
        if (!ValidKey(actor, key)) return Response<OperatorDraft>.Fail("Invalid draft reference.");
        return Response<OperatorDraft>.Success(await repository.GetAsync(actor, key, ct) ??
            new OperatorDraft { ActorSubject = actor, Key = key });
    }
    public async Task<Response<OperatorDraft>> SaveAsync(string actor, string key, Guid expectedVersion, string content, CancellationToken ct)
    {
        if (!ValidKey(actor, key) || !ValidContent(key, content))
            return Response<OperatorDraft>.Fail("Draft fields exceed their limits or contain unsupported data.");
        await repository.LockAsync(actor, ct);
        var draft = await repository.GetAsync(actor, key, ct);
        // Exact replay after a lost save response is safe; a stale different edit is not.
        if (draft is not null && draft.Content == content) return Response<OperatorDraft>.Success(draft);
        if ((draft?.Version ?? Guid.Empty) != expectedVersion)
            return Response<OperatorDraft>.Conflict("This draft was changed in another tab. Load the saved draft before editing it.", "draft_version_conflict");
        if (draft is null) { draft = new OperatorDraft { ActorSubject = actor, Key = key }; repository.Add(draft); }
        draft.Content = content; draft.Version = Guid.NewGuid(); draft.UpdatedAt = time.GetUtcNow();
        return Response<OperatorDraft>.Success(draft);
    }
    private static bool ValidKey(string actor, string key) => !string.IsNullOrWhiteSpace(actor) && actor.Length <= 320 &&
        (key == "workspace" || (key?.Split(':') is ["case" or "new-case" or "player" or "investigation" or "delivery", var id] && Guid.TryParse(id, out var target) && target != Guid.Empty));
    private static bool ValidContent(string key, string? content)
    {
        if (content is null || content.Length > 24000) return false;
        try
        {
            using var document = JsonDocument.Parse(content, new JsonDocumentOptions { MaxDepth = 2 });
            if (document.RootElement.ValueKind != JsonValueKind.Object) return false;
            var fields = key == "workspace" ? WorkspaceFields : key.StartsWith("player:") ? PlayerFields : key.StartsWith("investigation:") ? InvestigationFields : key.StartsWith("delivery:") ? RecoveryFields : CaseFields;
            var seen = new HashSet<string>();
            foreach (var property in document.RootElement.EnumerateObject())
                if (!seen.Add(property.Name) || !fields.TryGetValue(property.Name, out var limit) ||
                    property.Value.ValueKind != JsonValueKind.String || property.Value.GetString()!.Length > limit) return false;
            return true;
        }
        catch (JsonException) { return false; }
    }
}
