using Application.UseCases.Equipments.Commands.EquipEquipment;
using Application.UseCases.Equipments.Commands.UnequipEquipment;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Equipments.Queries.GetMyEquipment;
using Domain.Models.Items.Equipments.Slots;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public class EquipmentController : BaseController
{
    [HttpGet]
    public async Task<List<EquipmentSlotDto>> Get()
    {
        return await Mediator.Send(new GetMyEquipmentQuery(CurrentCharacterGuid));
    }

    //[HttpGet]
    //public async Task<IActionResult> GetAll()
    //{
    //    var query = new GetAllEquipmentQuery();
    //    var result = await _mediator.Send(query);
    //    return Ok(result);
    //}

    [HttpPost("Equip")]
    public async Task<bool> Equip([FromBody] string equipmentItemId)
    {
        return await Mediator.Send(new EquipEquipmentCommand(CurrentCharacterGuid, equipmentItemId));
    }

    [HttpPost("Unequip")]
    public async Task<bool> Unequip([FromBody] EquipmentType equipmentType)
    {
        return await Mediator.Send(new UnequipEquipmentCommand(CurrentCharacterGuid, equipmentType));
    }

    //[HttpDelete("{id}")]
    //public async Task<IActionResult> Delete(Guid id)
    //{
    //    var command = new DeleteEquipmentCommand { Id = id };
    //    await _mediator.Send(command);
    //    return NoContent();
    //}
}
