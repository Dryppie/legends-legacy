using Application.UseCases.Equipments.Commands.EquipEquipment;
using Application.UseCases.Equipments.Commands.UnequipEquipment;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Equipments.Queries.GetMyEquipment;
using Application.UseCases.Equipments.Queries.CompareEquipment;
using Application.UseCases.Equipments.Queries.PreviewEquipmentUpgrade;
using Application.UseCases.Equipments.Commands.ReinforceEquipment;
using Application.UseCases.Equipments.Commands.DismantleEquipment;
using Application.UseCases.Equipments.Commands.ApplyEquipmentVariant;
using Application.UseCases.Equipments.Queries.GetEquipmentBlueprints;
using Common.Primitives;
using Domain.Models.Items.Equipments.Slots;
using Microsoft.AspNetCore.Mvc;
using Domain.Models.Items.Equipments.Progression;

namespace API.LL.Controllers.V1;
public class EquipmentController : BaseController
{
    public record EquipEquipmentRequestDto(string EquipmentItemId, EquipmentSlotType? SlotType);
    public sealed record EquipmentUpgradePreviewRequestDto(
        EquipmentUpgradeOperationKind Kind,
        Guid ItemInstanceId,
        bool AllowFavoriteDismantle = false,
        string? BlueprintStyleId = null);
    public sealed record ApplyEquipmentVariantRequestDto(Guid OperationId, Guid ItemInstanceId,
        string BlueprintStyleId, string QuoteToken);
    public sealed record ReinforceEquipmentRequestDto(
        Guid OperationId,
        Guid ItemInstanceId,
        string QuoteToken);
    public sealed record DismantleEquipmentRequestDto(
        Guid OperationId,
        Guid ItemInstanceId,
        bool AllowFavoriteDismantle,
        string QuoteToken);
    [HttpGet]
    public async Task<ActionResult<List<EquipmentSlotDto>>> Get() =>
        await Mediator.Send(new GetMyEquipmentQuery(CurrentCharacterGuid));

    [HttpGet("comparison/{equipmentInstanceId:guid}")]
    public async Task<ActionResult<Response<EquipmentComparisonDto>>> Compare(
        Guid equipmentInstanceId,
        [FromQuery] EquipmentSlotType? slotType) =>
        await Mediator.Send(new CompareEquipmentQuery(
            CurrentCharacterGuid,
            equipmentInstanceId,
            slotType));

    [HttpPost("Equip")]
    public async Task<ActionResult<Response<EquipmentChangeResponseDto>>> Equip([FromBody] EquipEquipmentRequestDto equipmentRequestDto) =>
        await Mediator.Send(new EquipEquipmentCommand(CurrentCharacterGuid, equipmentRequestDto.EquipmentItemId, equipmentRequestDto.SlotType));

    [HttpPost("Unequip")]
    public async Task<ActionResult<Response<EquipmentChangeResponseDto>>> Unequip([FromBody] EquipmentSlotType slotType) =>
        await Mediator.Send(new UnequipEquipmentCommand(CurrentCharacterGuid, slotType));

    [HttpPost("upgrade/preview")]
    public async Task<ActionResult<EquipmentUpgradeQuoteDto>> PreviewUpgrade(
        [FromBody] EquipmentUpgradePreviewRequestDto request,
        CancellationToken cancellationToken) =>
        await Mediator.Send(new PreviewEquipmentUpgradeQuery(
            CurrentCharacterGuid,
            new EquipmentUpgradeRequest(
                request.Kind,
                request.ItemInstanceId,
                request.AllowFavoriteDismantle,
                request.BlueprintStyleId)), cancellationToken);

    [HttpGet("blueprints/{itemInstanceId:guid}")]
    public async Task<ActionResult<IReadOnlyList<EquipmentBlueprintOptionDto>>> Blueprints(Guid itemInstanceId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetEquipmentBlueprintsQuery(CurrentCharacterGuid, itemInstanceId), ct));

    [HttpPost("upgrade/variant")]
    public async Task<ActionResult<Response<EquipmentUpgradeMutationDto>>> ApplyVariant(
        [FromBody] ApplyEquipmentVariantRequestDto request, CancellationToken ct) =>
        await Mediator.Send(new ApplyEquipmentVariantCommand(CurrentCharacterGuid, request.OperationId,
            request.ItemInstanceId, request.BlueprintStyleId, request.QuoteToken), ct);

    [HttpPost("upgrade/reinforce")]
    public async Task<ActionResult<Response<EquipmentUpgradeMutationDto>>> Reinforce(
        [FromBody] ReinforceEquipmentRequestDto request,
        CancellationToken cancellationToken) =>
        await Mediator.Send(new ReinforceEquipmentCommand(
            CurrentCharacterGuid,
            request.OperationId,
            request.ItemInstanceId,
            request.QuoteToken), cancellationToken);

    [HttpPost("upgrade/dismantle")]
    public async Task<ActionResult<Response<EquipmentUpgradeMutationDto>>> Dismantle(
        [FromBody] DismantleEquipmentRequestDto request,
        CancellationToken cancellationToken) =>
        await Mediator.Send(new DismantleEquipmentCommand(
            CurrentCharacterGuid,
            request.OperationId,
            request.ItemInstanceId,
            request.AllowFavoriteDismantle,
            request.QuoteToken), cancellationToken);
}
