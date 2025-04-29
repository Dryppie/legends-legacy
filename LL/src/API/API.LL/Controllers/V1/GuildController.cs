using Application.UseCases.Guilds.Commands.CreateGuild;
using Application.UseCases.Guilds.Dtos;
using Application.UseCases.Guilds.Queries.GetAllGuilds;
using Application.UseCases.Guilds.Queries.GetMyGuild;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public class GuildController : BaseController
{
    [HttpGet("GetMyGuild")]
    public async Task<GuildDto?> GetMyGuild()
    {
        return await Mediator.Send(new GetMyGuildQuery(CurrentCharacterGuid));
    }

    [HttpGet("GetAllGuilds")]
    public async Task<List<GuildSimpleDto>> GetAllGuilds()
    {
        return await Mediator.Send(new GetAllGuildsQuery());
    }

    [HttpPost("CreateGuild")]
    public async Task<ActionResult> CreateGuild([FromBody] string name)
    {
        await Mediator.Send(new CreateGuildCommand(CurrentCharacterGuid, name));

        return Ok();
    }
}
