using Application.UseCases.Equipments.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public sealed class EquipmentAcquisitionController : BaseController
{
    [HttpGet("access")]
    public async Task<ActionResult<EquipmentAccessDto>> Access(CancellationToken ct) =>
        Ok(await Mediator.Send(new Application.UseCases.Equipments.Queries.GetEquipmentAccess.GetEquipmentAccessQuery(CurrentCharacterGuid), ct));
}
