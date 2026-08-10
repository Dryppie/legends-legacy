using Application.UseCases.Guilds.Commands.AcceptInvite;
using Application.UseCases.Guilds.Commands.ApplyToGuild;
using Application.UseCases.Guilds.Commands.ApproveApplication;
using Application.UseCases.Guilds.Commands.CreateGuild;
using Application.UseCases.Guilds.Commands.DisbandGuild;
using Application.UseCases.Guilds.Commands.Invite;
using Application.UseCases.Guilds.Commands.InviteCharacterByName;
using Application.UseCases.Guilds.Commands.LeaveGuild;
using Application.UseCases.Guilds.Commands.ConstructGuildBuilding;
using Application.UseCases.Guilds.Commands.ClaimGuildOrderReward;
using Application.UseCases.Guilds.Commands.ClaimGuildWeeklyMissionReward;
using Application.UseCases.Guilds.Commands.PurchaseGuildShopItem;
using Application.UseCases.Guilds.Commands.RejectApplication;
using Application.UseCases.Guilds.Commands.RejectInvite;
using Application.UseCases.Guilds.Commands.SelectGuildMission;
using Application.UseCases.Guilds.Commands.UpgradeGuildBuilding;
using Application.UseCases.Guilds.Commands.DonateGuildVaultItem;
using Application.UseCases.Guilds.Commands.BorrowGuildVaultItem;
using Application.UseCases.Guilds.Commands.ReturnGuildVaultItem;
using Application.UseCases.Guilds.Commands.WithdrawGuildVaultItem;
using Application.UseCases.Guilds.Commands.ChangeGuildMemberRole;
using Application.UseCases.Guilds.Commands.KickGuildMember;
using Application.UseCases.Guilds.Commands.UpdateGuildRolePermissions;
using Application.UseCases.Guilds.Dtos.Requests;
using Application.UseCases.Guilds.Dtos.Responses;
using Application.UseCases.Guilds.Queries.GetAllGuilds;
using Application.UseCases.Guilds.Queries.GetGuildMissions;
using Application.UseCases.Guilds.Queries.GetGuildShop;
using Application.UseCases.Guilds.Queries.GetGuildBuildings;
using Application.UseCases.Guilds.Queries.GetMyGuild;
using Application.UseCases.Guilds.Queries.GetMyInvites;
using Application.Interfaces.Services.LL.Guilds;
using Common.Primitives;
using Domain.Models.Guilds;
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

    [HttpGet("GetBuildings")]
    public async Task<GuildBuildingOverviewDto?> GetBuildings() =>
        await Mediator.Send(new GetGuildBuildingsQuery(CurrentCharacterGuid));

    [HttpPost("ConstructBuilding")]
    public async Task<ActionResult<Response<GuildBuildingOverviewDto>>> ConstructBuilding([FromBody] GuildBuildingType buildingType) =>
        await Mediator.Send(new ConstructGuildBuildingCommand(CurrentCharacterGuid, buildingType));

    [HttpPost("UpgradeBuilding")]
    public async Task<ActionResult<Response<GuildBuildingOverviewDto>>> UpgradeBuilding([FromBody] string id) =>
        await Mediator.Send(new UpgradeGuildBuildingCommand(CurrentCharacterGuid, Guid.Parse(id)));

    [HttpGet("GetMissions")]
    public async Task<GuildMissionOverviewDto?> GetMissions() =>
        await Mediator.Send(new GetGuildMissionsQuery(CurrentCharacterGuid));

    [HttpPost("SelectMission")]
    public async Task<ActionResult<Response<GuildMissionOverviewDto>>> SelectMission([FromBody] string missionOptionId) =>
        await Mediator.Send(new SelectGuildMissionCommand(CurrentCharacterGuid, Guid.Parse(missionOptionId)));

    [HttpPost("ClaimOrderReward")]
    public async Task<ActionResult<Response<GuildMissionOverviewDto>>> ClaimOrderReward([FromBody] string orderId) =>
        await Mediator.Send(new ClaimGuildOrderRewardCommand(CurrentCharacterGuid, Guid.Parse(orderId)));

    [HttpPost("ClaimWeeklyMissionReward")]
    public async Task<ActionResult<Response<GuildMissionOverviewDto>>> ClaimWeeklyMissionReward() =>
        await Mediator.Send(new ClaimGuildWeeklyMissionRewardCommand(CurrentCharacterGuid));

    [HttpGet("GetShop")]
    public async Task<GuildShopOverviewDto?> GetShop() =>
        await Mediator.Send(new GetGuildShopQuery(CurrentCharacterGuid));

    [HttpPost("PurchaseShopItem")]
    public async Task<ActionResult<Response<GuildShopOverviewDto>>> PurchaseShopItem([FromBody] string itemKey) =>
        await Mediator.Send(new PurchaseGuildShopItemCommand(CurrentCharacterGuid, itemKey));

    [HttpPost("DonateVaultItem")]
    public async Task<ActionResult<Response<bool>>> DonateVaultItem([FromBody] Guid equipmentInstanceId) =>
        await Mediator.Send(new DonateGuildVaultItemCommand(CurrentCharacterGuid, equipmentInstanceId));

    [HttpPost("BorrowVaultItem")]
    public async Task<ActionResult<Response<bool>>> BorrowVaultItem([FromBody] Guid vaultItemId) =>
        await Mediator.Send(new BorrowGuildVaultItemCommand(CurrentCharacterGuid, vaultItemId));

    [HttpPost("ReturnVaultItem")]
    public async Task<ActionResult<Response<bool>>> ReturnVaultItem([FromBody] Guid vaultItemId) =>
        await Mediator.Send(new ReturnGuildVaultItemCommand(CurrentCharacterGuid, vaultItemId));

    [HttpPost("WithdrawVaultItem")]
    public async Task<ActionResult<Response<bool>>> WithdrawVaultItem([FromBody] Guid vaultItemId) =>
        await Mediator.Send(new WithdrawGuildVaultItemCommand(CurrentCharacterGuid, vaultItemId));

    [HttpPost("ChangeMemberRole")]
    public async Task<ActionResult<Response<bool>>> ChangeMemberRole([FromBody] ChangeGuildMemberRoleDto request) =>
        await Mediator.Send(new ChangeGuildMemberRoleCommand(CurrentCharacterGuid, request));

    [HttpPost("KickMember")]
    public async Task<ActionResult<Response<bool>>> KickMember([FromBody] Guid characterId) =>
        await Mediator.Send(new KickGuildMemberCommand(CurrentCharacterGuid, characterId));

    [HttpPost("UpdateRolePermissions")]
    public async Task<ActionResult<Response<bool>>> UpdateRolePermissions([FromBody] UpdateGuildRolePermissionsDto request) =>
        await Mediator.Send(new UpdateGuildRolePermissionsCommand(CurrentCharacterGuid, request));
}
