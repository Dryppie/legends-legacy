using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Threading.Tasks;
using System;

namespace API.LL.Controllers.V1;
public class EquipmentController : BaseController
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get()
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

    //[HttpPost]
    //public async Task<IActionResult> Create([FromBody] CreateEquipmentCommand command)
    //{
    //    var result = await _mediator.Send(command);
    //    return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    //}

    //[HttpDelete("{id}")]
    //public async Task<IActionResult> Delete(Guid id)
    //{
    //    var command = new DeleteEquipmentCommand { Id = id };
    //    await _mediator.Send(command);
    //    return NoContent();
    //}
}
