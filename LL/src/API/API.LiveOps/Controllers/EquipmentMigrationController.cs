using Application.UseCases.Administration;
using Application.UseCases.Equipments.Commands.ApplyEquipmentMigration;
using Application.UseCases.Equipments.Commands.RollbackEquipmentMigration;
using Application.UseCases.Equipments.Queries.AuditEquipmentMigration;
using Application.UseCases.Equipments.Queries.PreviewEquipmentMigration;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/equipment-migration")]
[Authorize(Policy = AdministrationPermissions.SuperAdmin)]
public sealed class EquipmentMigrationController : LiveOpsControllerBase
{
    public sealed record PreviewRequest(EquipmentMigrationTarget Target, string? DefinitionId, int? TargetBalanceVersion = null);
    public sealed record ApplyRequest(Guid OperationId, EquipmentMigrationTarget Target, string SourceHash, string DefinitionId, int? TargetBalanceVersion = null, string? ExpectedResultHash = null);

    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery] int page = 0, [FromQuery] int pageSize = 100, [FromQuery] int sourceBalanceVersion = 1, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new AuditEquipmentMigrationQuery(page, pageSize, sourceBalanceVersion), ct));

    [HttpPost("preview")]
    public async Task<IActionResult> Preview(PreviewRequest request, CancellationToken ct) =>
        Ok(await Mediator.Send(new PreviewEquipmentMigrationQuery(request.Target, request.DefinitionId, request.TargetBalanceVersion), ct));

    [HttpPost("apply")]
    public async Task<IActionResult> Apply(ApplyRequest request, CancellationToken ct) =>
        Ok(await Mediator.Send(new ApplyEquipmentMigrationCommand(request.OperationId, request.Target, request.SourceHash, request.DefinitionId, CurrentActor.Subject, request.TargetBalanceVersion, request.ExpectedResultHash), ct));

    [HttpPost("{operationId:guid}/rollback")]
    public async Task<IActionResult> Rollback(Guid operationId, CancellationToken ct) =>
        Ok(await Mediator.Send(new RollbackEquipmentMigrationCommand(operationId, CurrentActor.Subject), ct));
}
