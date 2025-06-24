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
using Application.UseCases.Guilds.Queries.GetGuildUpgrades;
using Application.UseCases.Guilds.Queries.GetMyGuild;
using Application.UseCases.Guilds.Queries.GetMyInvites;
using Common.Primitives;
using Domain.Models.Guilds.Buildings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
public class GuildController : BaseController
{
    [HttpGet("GetMyGuild")]
    public async Task<GuildDto?> GetMyGuild() =>
        await Mediator.Send(new GetMyGuildQuery(CurrentCharacterGuid));

    [HttpGet("GetAllGuilds")]
    public async Task<List<GuildSimpleDto>> GetAllGuilds() =>
        await Mediator.Send(new GetAllGuildsQuery());

    [HttpPost("CreateGuild")]
    public async Task<ActionResult<Response<bool>>> CreateGuild([FromBody] string name) =>
        await Mediator.Send(new CreateGuildCommand(CurrentCharacterGuid, name));

    [HttpGet("GetMyInvites")]
    public async Task<List<GuildInviteDto>> GetMyInvites() =>
        await Mediator.Send(new GetMyInvitesQuery(CurrentCharacterGuid));


    [HttpPost("Invite")]
    public async Task<ActionResult<Response<bool>>> Invite([FromBody] InviteToGuildDto invite) =>
        await Mediator.Send(new InviteCommand(CurrentCharacterGuid, invite));

    [HttpPost("InviteCharacterByName")]
    public async Task<ActionResult<Response<bool>>> InviteCharacterByName([FromBody] InviteToGuildDto invite) => 
        await Mediator.Send(new InviteCharacterByNameCommand(CurrentCharacterGuid, invite));

    [HttpPost("AcceptInvite")]
    public async Task<ActionResult<Response<bool>>> AcceptInvite([FromBody] string guildId) => 
        await Mediator.Send(new AcceptInviteCommand(CurrentCharacterGuid, guildId));

    [HttpPost("RejectInvite")]
    public async Task<ActionResult<Response<bool>>> RejectInvite([FromBody] string guildId) => 
        await Mediator.Send(new RejectInviteCommand(CurrentCharacterGuid, guildId));

    [HttpPost("ApproveApplication")]
    public async Task<ActionResult<Response<bool>>> ApproveApplication([FromBody] string characterId) => 
        await Mediator.Send(new ApproveApplicationCommand(CurrentCharacterGuid, characterId));

    [HttpPost("RejectApplication")]
    public async Task<ActionResult<Response<bool>>> RejectApplication([FromBody] string characterId) => 
        await Mediator.Send(new RejectApplicationCommand(CurrentCharacterGuid, characterId));

    [HttpPost("ApplyToGuild")]
    public async Task<ActionResult<Response<bool>>> ApplyToGuild([FromBody] string guildId) => 
        await Mediator.Send(new ApplyToGuildCommand(CurrentCharacterGuid, guildId));

    [HttpPost("LeaveGuild")]
    public async Task<ActionResult<Response<bool>>> LeaveGuild() => 
        await Mediator.Send(new LeaveGuildCommand(CurrentCharacterGuid));

    [HttpPost("DisbandGuild")]
    public async Task<ActionResult<Response<bool>>> DisbandGuild() =>
        await Mediator.Send(new DisbandGuildCommand(CurrentCharacterGuid));

    [HttpGet("GetUpgrades")]
    public async Task<ActionResult<Response<List<BuildingUpgradeView>>>> GetUpgrades() =>
        await Mediator.Send(new GetGuildUpgradesQuery(CurrentCharacterGuid));
}
