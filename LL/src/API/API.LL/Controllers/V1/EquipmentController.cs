using Application.UseCases.Equipments.Commands.EquipEquipment;
using Application.UseCases.Equipments.Commands.UnequipEquipment;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Equipments.Queries.GetMyEquipment;
using Application.UseCases.Equipments.Queries.CompareEquipment;
using Common.Primitives;
using Domain.Models.Items.Equipments.Slots;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public class EquipmentController : BaseController
{
    public record EquipEquipmentRequestDto(string EquipmentItemId, EquipmentSlotType? SlotType);
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
}
