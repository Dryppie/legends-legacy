using Application.UseCases.Soulstones.Commands.PurchaseSoulstoneUpgrade;
using Application.UseCases.Soulstones.Commands.ResetSoulstoneUpgrades;
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
    public async Task<ActionResult<Response<SoulstoneUpgradeMutationResult>>> Upgrade([FromBody] string soulstoneUpgradeId) =>
        await Mediator.Send(new PurchaseSoulstoneUpgradeCommand(CurrentCharacterGuid, soulstoneUpgradeId));

    [HttpPost("Reset")]
    public async Task<ActionResult<Response<SoulstoneUpgradeMutationResult>>> Reset() =>
        await Mediator.Send(new ResetSoulstoneUpgradesCommand(CurrentCharacterGuid));
}
