using Application.UseCases.Soulstones.Commands;
using Application.UseCases.Soulstones.Queries;
using Common.Primitives;
using Domain.Models.Soulstones.UpgradeDefinition;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public class SoulstoneUpgradeController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<Response<List<SoulstoneUpgradeView>>>> Get() =>
        await Mediator.Send(new GetMySoulstoneUpgradesQuery(CurrentCharacterGuid));

    [HttpPost("Upgrade")]
    public async Task<ActionResult<Response<bool>>> Upgrade([FromBody] string soulstoneUpgradeId) =>
        await Mediator.Send(new PurchaseSoulstoneUpgradeCommand(CurrentCharacterGuid, soulstoneUpgradeId));
}
