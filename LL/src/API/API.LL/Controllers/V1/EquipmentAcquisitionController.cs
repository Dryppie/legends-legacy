using Application.UseCases.Equipments.Commands.RecoverBaselineEquipment;
using Application.UseCases.Equipments.Commands.SelectEquipmentProgressionTarget;
using Application.UseCases.Equipments.Queries.GetBaselineEquipmentRecovery;
using Application.UseCases.Equipments.Queries.GetEquipmentProtectionPools;
using Application.UseCases.Equipments.Dtos;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public sealed class EquipmentAcquisitionController : BaseController
{
    public sealed record PlainRecoveryRequest(Guid OperationId, string DefinitionId, int Tier);
    [HttpGet("plain-recovery")]
    public async Task<ActionResult<IReadOnlyList<PlainEquipmentRecoveryOptionDto>>> PlainRecovery(CancellationToken ct) =>
        Ok(await Mediator.Send(new Application.UseCases.Equipments.Queries.GetPlainEquipmentRecovery.GetPlainEquipmentRecoveryQuery(CurrentCharacterGuid), ct));
    [HttpPost("plain-recovery")]
    public async Task<ActionResult<Response<PlainEquipmentRecoveryDto>>> RecoverPlain(PlainRecoveryRequest request, CancellationToken ct) =>
        await Mediator.Send(new Application.UseCases.Equipments.Commands.RecoverPlainEquipment.RecoverPlainEquipmentCommand(
            CurrentCharacterGuid, request.OperationId, request.DefinitionId, request.Tier), ct);
    [HttpGet("access")]
    public async Task<ActionResult<EquipmentAccessDto>> Access(CancellationToken ct) =>
        Ok(await Mediator.Send(new Application.UseCases.Equipments.Queries.GetEquipmentAccess.GetEquipmentAccessQuery(CurrentCharacterGuid), ct));

    public sealed record OrdinaryRequest(Guid OperationId, string PoolId, string? DefinitionId, string? SigilFamilyId);
    [HttpGet("ordinary")]
    public async Task<ActionResult<IReadOnlyList<CombatAcquisitionDto>>> Ordinary(CancellationToken ct) =>
        Ok(await Mediator.Send(new Application.UseCases.Equipments.Queries.GetCombatAcquisition.GetCombatAcquisitionQuery(CurrentCharacterGuid), ct));
    [HttpPost("ordinary")]
    public async Task<ActionResult<Response<CombatAcquisitionDto>>> SelectOrdinary(OrdinaryRequest request, CancellationToken ct) =>
        await Mediator.Send(new Application.UseCases.Equipments.Commands.SelectCombatAcquisition.SelectCombatAcquisitionCommand(
            CurrentCharacterGuid, request.OperationId, request.PoolId, request.DefinitionId, request.SigilFamilyId), ct);
    public sealed record TargetRequest(string PoolId, string? DefinitionId);
    public sealed record RecoveryRequest(Guid OperationId, StarterEquipmentGrantKind Kind);
    [HttpGet("sources")]
    public async Task<ActionResult<IReadOnlyList<EquipmentProtectionPoolDto>>> Sources(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetEquipmentProtectionPoolsQuery(CurrentCharacterGuid), ct));
    [HttpPost("target")]
    public async Task<ActionResult<Response<EquipmentProtectionPoolDto>>> Target(TargetRequest request, CancellationToken ct) =>
        await Mediator.Send(new SelectEquipmentProgressionTargetCommand(CurrentCharacterGuid, request.PoolId, request.DefinitionId), ct);
    [HttpGet("recovery")]
    public async Task<ActionResult<IReadOnlyList<BaselineEquipmentRecoveryOptionDto>>> Recovery(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetBaselineEquipmentRecoveryQuery(CurrentCharacterGuid), ct));
    [HttpPost("recovery")]
    public async Task<ActionResult<Response<BaselineEquipmentRecoveryDto>>> Recover(RecoveryRequest request, CancellationToken ct) =>
        await Mediator.Send(new RecoverBaselineEquipmentCommand(CurrentCharacterGuid, request.OperationId, request.Kind), ct);
}
