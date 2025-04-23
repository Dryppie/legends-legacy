using Application.UseCases.CharacterActions.Dtos.CombatDtos;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Colosseum.Commands.StartArenaBattle;
using Application.UseCases.Colosseum.Queries.GetArenaOpponents;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public class ColosseumController : BaseController
{
    [HttpGet("GetArenaOpponents")]
    public async Task<List<CharacterDto>> Get()
    {
        return await Mediator.Send(new GetArenaOpponentsQuery(CurrentCharacterGuid));
    }

    [HttpPost("StartArenaBattle")]
    public async Task<CombatResultDto> Equip([FromBody] string enemyId)
    {
        return await Mediator.Send(new StartArenaBattleCommand(CurrentCharacterGuid, Guid.Parse(enemyId)));
    }
}
