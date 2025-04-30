using Application.UseCases.Guilds.Commands.AcceptInvite;
using Application.UseCases.Guilds.Commands.CreateGuild;
using Application.UseCases.Guilds.Commands.Invite;
using Application.UseCases.Guilds.Dtos;
using Application.UseCases.Guilds.Queries.GetAllGuilds;
using Application.UseCases.Guilds.Queries.GetMyGuild;
using Application.UseCases.Guilds.Queries.GetMyInvites;
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

    [HttpGet("GetMyInvites")]
    public async Task<List<GuildInviteDto>> GetMyInvites()
    {
        return await Mediator.Send(new GetMyInvitesQuery(CurrentCharacterGuid));
    }

    [HttpPost("Invite")]
    public async Task<ActionResult> Invite([FromBody] string guildId)
    {
        await Mediator.Send(new InviteCommand(CurrentCharacterGuid, guildId, "a4796361-9606-43e8-a7ab-4b4ea4be60ae"));

        return Ok();
    }

    [HttpPost("AcceptInvite")]
    public async Task<ActionResult> AcceptInvite([FromBody] string guildId)
    {
        await Mediator.Send(new AcceptInviteCommand(CurrentCharacterGuid, guildId));

        return Ok();
    }
}
