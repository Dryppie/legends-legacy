using Application.UseCases.Guilds.Commands.AcceptInvite;
using Application.UseCases.Guilds.Commands.ApplyToGuild;
using Application.UseCases.Guilds.Commands.ApproveApplication;
using Application.UseCases.Guilds.Commands.CreateGuild;
using Application.UseCases.Guilds.Commands.DisbandGuild;
using Application.UseCases.Guilds.Commands.Invite;
using Application.UseCases.Guilds.Commands.InviteCharacterByName;
using Application.UseCases.Guilds.Commands.LeaveGuild;
using Application.UseCases.Guilds.Commands.RejectApplication;
using Application.UseCases.Guilds.Commands.RejectInvite;
using Application.UseCases.Guilds.Dtos.Requests;
using Application.UseCases.Guilds.Dtos.Responses;
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
    public async Task<ActionResult> Invite([FromBody] InviteToGuildDto invite)
    {
        await Mediator.Send(new InviteCommand(CurrentCharacterGuid, invite));

        return Ok();
    }

    [HttpPost("InviteCharacterByName")]
    public async Task<ActionResult> InviteCharacterByName([FromBody] InviteToGuildDto invite)
    {
        await Mediator.Send(new InviteCharacterByNameCommand(CurrentCharacterGuid, invite));

        return Ok();
    }

    [HttpPost("AcceptInvite")]
    public async Task<ActionResult> AcceptInvite([FromBody] string guildId)
    {
        await Mediator.Send(new AcceptInviteCommand(CurrentCharacterGuid, guildId));

        return Ok();
    }

    [HttpPost("RejectInvite")]
    public async Task<ActionResult> RejectInvite([FromBody] string guildId)
    {
        await Mediator.Send(new RejectInviteCommand(CurrentCharacterGuid, guildId));

        return Ok();
    }

    [HttpPost("ApproveApplication")]
    public async Task<ActionResult> ApproveApplication([FromBody] string characterId)
    {
        await Mediator.Send(new ApproveApplicationCommand(CurrentCharacterGuid, characterId));

        return Ok();
    }

    [HttpPost("RejectApplication")]
    public async Task<ActionResult> RejectApplication([FromBody] string characterId)
    {
        await Mediator.Send(new RejectApplicationCommand(CurrentCharacterGuid, characterId));

        return Ok();
    }

    [HttpPost("ApplyToGuild")]
    public async Task<ActionResult> ApplyToGuild([FromBody] string guildId)
    {
        await Mediator.Send(new ApplyToGuildCommand(CurrentCharacterGuid, guildId));

        return Ok();
    }

    [HttpPost("LeaveGuild")]
    public async Task<ActionResult> LeaveGuild()
    {
        await Mediator.Send(new LeaveGuildCommand(CurrentCharacterGuid));

        return Ok();
    }

    [HttpPost("DisbandGuild")]
    public async Task<ActionResult> DisbandGuild()
    {
        await Mediator.Send(new DisbandGuildCommand(CurrentCharacterGuid));

        return Ok();
    }
}
