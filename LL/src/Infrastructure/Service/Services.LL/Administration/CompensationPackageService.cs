using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces.Services.LL.Administration;
using Application.UseCases.Administration;
using Common.Primitives;
using Domain.Models.Administration;
using Domain.Models.Nobility;
using Microsoft.Extensions.Options;
namespace Services.LL.Administration;

public sealed class CompensationPackageService(ICompensationPackageRepository repository, IAdministrationRepository administration,
    ILiveOpsService liveOps, IOptions<LiveOpsOptions> options, TimeProvider time) : ICompensationPackageService
{
    public async Task<IReadOnlyList<CompensationPackageDefinition>> ListAsync(CancellationToken ct) =>
        (await repository.ListAsync(ct)).Select(Definition).ToArray();
    public async Task<Response<CompensationPackageDefinition>> SaveAsync(CompensationPackageEdit input, AdministrationActor actor, CancellationToken ct)
    {
        var edit = input with { Name = input.Name?.Trim() ?? "", Purpose = input.Purpose?.Trim() ?? "" };
        if (edit.OperationId == Guid.Empty || edit.PackageId == Guid.Empty || edit.Name.Length is < 3 or > 120 || edit.Purpose.Length is < 3 or > 1000 ||
            string.IsNullOrWhiteSpace(actor.Subject) || actor.Subject.Length > 320)
            return Response<CompensationPackageDefinition>.Fail("Supply a name (3–120 characters), purpose (3–1000 characters) and valid operation and package references.");
        var hash = Hash(edit);
        await repository.LockAsync(edit.PackageId, ct);
        var previous = await repository.FindOperationAsync(edit.OperationId, ct);
        if (previous is not null)
            return previous.RequestHash == hash && previous.ActorSubject == actor.Subject
                ? Response<CompensationPackageDefinition>.Success(Definition(previous))
                : Response<CompensationPackageDefinition>.Conflict("This operation reference belongs to another package edit.", "package_operation_conflict");
        if (await administration.GetActionAsync(edit.OperationId, ct) is not null)
            return Response<CompensationPackageDefinition>.Conflict("Operation reference already used.", "package_operation_conflict");
        var latest = await repository.LatestAsync(edit.PackageId, ct);
        if ((latest?.Version ?? 0) != edit.ExpectedVersion)
            return Response<CompensationPackageDefinition>.Conflict("The package changed. Reload its current version before saving.", "package_version_conflict");
        var validation = await ValidateLinesAsync(edit.OperationId, edit.CharacterId, edit.Items, ct);
        if (!validation.IsSuccess) return Response<CompensationPackageDefinition>.Fail(validation.ErrorMessage);
        var version = new CompensationPackageVersion { Id = edit.OperationId, PackageId = edit.PackageId,
            Version = edit.ExpectedVersion + 1, Name = edit.Name, Purpose = edit.Purpose, Archived = edit.Archived,
            ItemsJson = JsonSerializer.Serialize(edit.Items), RequestHash = hash, ActorSubject = actor.Subject, CreatedAt = time.GetUtcNow() };
        repository.Add(version);
        administration.AddAction(new AdminAction { Id = edit.OperationId, ActionType = AdminActionType.CompensationPackageSaved,
            Permission = AdministrationPermissions.EconomyCompensation, ActorSubject = actor.Subject, ActorDisplayName = actor.DisplayName,
            TargetResourceId = edit.PackageId, Reason = edit.Purpose, OccurredAt = version.CreatedAt,
            DetailsJson = JsonSerializer.Serialize(new { version.PackageId, version.Version, version.Name, version.Archived, Items = edit.Items }) });
        return Response<CompensationPackageDefinition>.Success(Definition(version));
    }
    public async Task<Response<CompensationPackagePlan>> PrepareAsync(Guid operationId, Guid characterId, Guid packageId, int version, CancellationToken ct)
    {
        var latest = await repository.LatestAsync(packageId, ct);
        if (latest is null || latest.Archived || latest.Version != version)
            return Response<CompensationPackagePlan>.Conflict("The package changed or was archived. Select and review its current version.", "package_version_conflict");
        var player = await liveOps.GetPlayerAsync(characterId, ct);
        if (player is null) return Response<CompensationPackagePlan>.Fail("The player was not found.");
        var definition = Definition(latest);
        var lines = await ValidateLinesAsync(operationId, characterId, definition.Items, ct);
        return lines.IsSuccess ? Response<CompensationPackagePlan>.Success(new(definition, player, lines.Data!))
            : Response<CompensationPackagePlan>.Fail(lines.ErrorMessage);
    }
    public async Task<Response<CompensationPackageOperation>> GrantAsync(Guid operationId, Guid characterId, Guid packageId, int version,
        AdministrationActor actor, string reason, string? notes, CancellationToken ct)
    {
        reason = reason?.Trim() ?? ""; notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (operationId == Guid.Empty || reason.Length is < 1 or > 1000 || notes?.Length > 4000)
            return Response<CompensationPackageOperation>.Fail("Supply a valid operation, reason and bounded internal notes.");
        await repository.LockAsync(packageId, ct);
        var hash = Hash(new { CharacterId = characterId, PackageId = packageId, Version = version, Reason = reason, Notes = notes });
        var existing = await administration.GetActionAsync(operationId, ct);
        if (existing is not null)
        {
            var receipt = existing.ActionType == AdminActionType.CompensationPackageGranted ? JsonSerializer.Deserialize<PackageReceipt>(existing.DetailsJson) : null;
            return receipt?.RequestHash == hash && existing.ActorSubject == actor.Subject
                ? Response<CompensationPackageOperation>.Success(new(existing, [], true))
                : Response<CompensationPackageOperation>.Conflict("Operation reference already belongs to a different request.", "package_operation_conflict");
        }
        var prepared = await PrepareAsync(operationId, characterId, packageId, version, ct);
        if (!prepared.IsSuccess || prepared.Data is null) return Response<CompensationPackageOperation>.Conflict(prepared.ErrorMessage, "package_preview_stale");
        var plan = prepared.Data;
        var grants = new List<ItemGrantOperation>();
        for (var i = 0; i < plan.Package.Items.Count; i++)
        {
            var line = plan.Package.Items[i];
            var result = await liveOps.GrantCompensationItemsAsync(ChildOperation(operationId, i), characterId, actor,
                line.ItemBaseId, line.Quantity, reason, notes, ct, line.Equipment);
            // Throw rather than return a failed response after any write, so the parent command rolls back all lines.
            if (!result.IsSuccess || result.Value is null) throw new CompensationPackageConflictException(result.ErrorMessage);
            grants.Add(result.Value);
        }
        var action = new AdminAction { Id = operationId, ActionType = AdminActionType.CompensationPackageGranted,
            Permission = AdministrationPermissions.EconomyCompensation, ActorSubject = actor.Subject, ActorDisplayName = actor.DisplayName,
            TargetAccountId = plan.Player.AccountId, TargetCharacterId = characterId, TargetResourceId = packageId,
            Reason = reason, InternalNotes = notes, RiskLevel = AdministrationRiskLevel.HighValue, OccurredAt = time.GetUtcNow(),
            DetailsJson = JsonSerializer.Serialize(new PackageReceipt(hash, packageId, version, plan.Package.Name, grants.Select(x => x.Action.Id).ToArray())) };
        administration.AddAction(action);
        return Response<CompensationPackageOperation>.Success(new(action, grants, false));
    }
    private async Task<Response<IReadOnlyList<CompensationGrantPlan>>> ValidateLinesAsync(Guid operationId, Guid characterId,
        IReadOnlyList<CompensationPackageLine>? lines, CancellationToken ct)
    {
        if (lines is null || lines.Count is < 1 or > 10 || lines.Any(x => x is null || string.IsNullOrWhiteSpace(x.ItemBaseId) || x.Quantity < 1 || x.ItemBaseId == NobilityBenefits.SignetItemId) ||
            lines.Sum(x => (long)x.Quantity) > options.Value.MaximumGrantQuantity)
            return Response<IReadOnlyList<CompensationGrantPlan>>.Fail("A package needs 1–10 item lines within the total compensation cap. Signets use their separate reviewed grant.");
        var plans = new List<CompensationGrantPlan>();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var result = await liveOps.PrepareCompensationGrantAsync(ChildOperation(operationId, i), characterId, line.ItemBaseId, line.Quantity, ct, line.Equipment);
            if (!result.IsSuccess || result.Value is null) return Response<IReadOnlyList<CompensationGrantPlan>>.Fail($"Line {i + 1}: {result.ErrorMessage}");
            plans.Add(result.Value);
        }
        return Response<IReadOnlyList<CompensationGrantPlan>>.Success(plans);
    }
    public static Guid ChildOperation(Guid operationId, int index) => new(SHA256.HashData(Encoding.UTF8.GetBytes($"liveops-package:{operationId:D}:{index}"))[..16]);
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static CompensationPackageDefinition Definition(CompensationPackageVersion value) => new(value.PackageId, value.Version, value.Name,
        value.Purpose, value.Archived, JsonSerializer.Deserialize<CompensationPackageLine[]>(value.ItemsJson)!, value.CreatedAt);
    private sealed record PackageReceipt(string RequestHash, Guid PackageId, int Version, string Name, Guid[] ChildOperations);
}
