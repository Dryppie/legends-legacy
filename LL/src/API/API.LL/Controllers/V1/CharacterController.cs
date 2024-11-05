using Application.UseCases.Characters.Dtos;
using Application.UseCases.Characters.Queries.GetCharacter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
public class CharacterController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<CharacterDto>> Get()
    {
        var character = await Mediator.Send(new GetCharacterQuery(CurrentUserId));
        
        return Ok(character);
    }
}
