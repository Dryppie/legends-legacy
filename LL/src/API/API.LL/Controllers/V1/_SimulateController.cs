using Application.UseCases._Simulates;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

public class _SimulateController : BaseController
{
    [HttpPost("SimulateCombat")]
    public async Task<IActionResult> SimulateCombat(int PlayerTeamSize = 1, int EnemyTeamSize = 1, int Fights = 1, int Tier = 1, int LocationId = 0)
    {
        await Mediator.Send(new SimulateCombatCommand(PlayerTeamSize, EnemyTeamSize, Fights, Tier, LocationId));

        return Ok();
    }
}
