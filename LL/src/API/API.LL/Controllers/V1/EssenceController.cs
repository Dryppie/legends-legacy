using Application.UseCases.Essences.Commands.AbsorbUnboundEssence;
using Application.UseCases.Essences.Commands.ActivateEssenceLoadout;
using Application.UseCases.Essences.Commands.AscendEssence;
using Application.UseCases.Essences.Commands.DeleteEssenceLoadout;
using Application.UseCases.Essences.Commands.DismantleUnboundEssence;
using Application.UseCases.Essences.Commands.EvolveEssence;
using Application.UseCases.Essences.Commands.FavoriteEssence;
using Application.UseCases.Essences.Commands.SaveEssenceLoadout;
using Application.UseCases.Essences.Commands.SetEssenceFocus;
using Application.UseCases.Essences.Commands.SpendEssenceDust;
using Application.UseCases.Essences.Commands.UpgradeEssencePotential;
using Application.UseCases.Essences.Dtos;
using Application.UseCases.Essences.Queries.GetActiveEssenceLoadout;
using Application.UseCases.Essences.Queries.GetCreatureArchive;
using Application.UseCases.Essences.Queries.GetEssenceCodex;
using Application.UseCases.Essences.Queries.GetEssenceLoadouts;
using Application.UseCases.Essences.Queries.GetSoulArchive;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

public class EssenceController : BaseController
{
    [HttpGet("archive")]
    public async Task<ActionResult<SoulArchiveDto>> GetArchive() =>
        await Mediator.Send(new GetSoulArchiveQuery(CurrentCharacterGuid));

    [HttpGet("creatures")]
    public async Task<ActionResult<CreatureArchiveDto>> GetCreatureArchive() =>
        await Mediator.Send(new GetCreatureArchiveQuery(CurrentCharacterGuid));

    [HttpGet("codex")]
    public async Task<ActionResult<EssenceCodexDto>> GetCodex() =>
        await Mediator.Send(new GetEssenceCodexQuery(CurrentCharacterGuid));

    [HttpPost("creatures/focus")]
    public async Task<ActionResult<CreatureArchiveDto>> SetEssenceFocus([FromBody] SetEssenceFocusRequestDto request) =>
        await Mediator.Send(new SetEssenceFocusCommand(CurrentCharacterGuid, request.CreatureId));

    [HttpGet("loadouts")]
    public async Task<ActionResult<EssenceLoadoutsDto>> GetLoadouts() =>
        await Mediator.Send(new GetEssenceLoadoutsQuery(CurrentCharacterGuid));

    [HttpGet("loadouts/active")]
    public async Task<ActionResult<EssenceLoadoutDto?>> GetActiveLoadout() =>
        await Mediator.Send(new GetActiveEssenceLoadoutQuery(CurrentCharacterGuid));

    [HttpPost("items/{inventoryItemId:guid}/absorb")]
    public async Task<ActionResult<Response<EssenceMutationResponseDto>>> AbsorbUnboundEssence(Guid inventoryItemId) =>
        await Mediator.Send(new AbsorbUnboundEssenceCommand(CurrentCharacterGuid, inventoryItemId));

    [HttpPost("items/{inventoryItemId:guid}/dismantle")]
    public async Task<ActionResult<Response<EssenceMutationResponseDto>>> DismantleUnboundEssence(Guid inventoryItemId) =>
        await Mediator.Send(new DismantleUnboundEssenceCommand(CurrentCharacterGuid, inventoryItemId));

    [HttpPost("{playerEssenceId:guid}/spend-dust")]
    public async Task<ActionResult<Response<EssenceMutationResponseDto>>> SpendDust(Guid playerEssenceId, [FromBody] SpendEssenceDustRequestDto request) =>
        await Mediator.Send(new SpendEssenceDustCommand(CurrentCharacterGuid, playerEssenceId, request.DustAmount));

    [HttpPost("{playerEssenceId:guid}/ascend")]
    public async Task<ActionResult<Response<EssenceMutationResponseDto>>> Ascend(Guid playerEssenceId) =>
        await Mediator.Send(new AscendEssenceCommand(CurrentCharacterGuid, playerEssenceId));

    [HttpPost("{playerEssenceId:guid}/potential/upgrade")]
    public async Task<ActionResult<Response<EssenceMutationResponseDto>>> UpgradePotential(Guid playerEssenceId) =>
        await Mediator.Send(new UpgradeEssencePotentialCommand(CurrentCharacterGuid, playerEssenceId));

    [HttpPost("{playerEssenceId:guid}/evolve")]
    public async Task<ActionResult<Response<EssenceMutationResponseDto>>> Evolve(Guid playerEssenceId) =>
        await Mediator.Send(new EvolveEssenceCommand(CurrentCharacterGuid, playerEssenceId));

    [HttpPost("{playerEssenceId:guid}/favorite")]
    public async Task<ActionResult<Response<ResponseMessageDto>>> Favorite(Guid playerEssenceId, [FromBody] SetFavoriteEssenceRequestDto request) =>
        await Mediator.Send(new FavoriteEssenceCommand(CurrentCharacterGuid, playerEssenceId, request.IsFavorite));

    [HttpPost("loadouts")]
    public async Task<ActionResult<Response<EssenceLoadoutDto>>> SaveLoadout([FromBody] SaveEssenceLoadoutDto request) =>
        await Mediator.Send(new SaveEssenceLoadoutCommand(CurrentCharacterGuid, request));

    [HttpPut("loadouts/{loadoutId:guid}")]
    public async Task<ActionResult<Response<EssenceLoadoutDto>>> UpdateLoadout(Guid loadoutId, [FromBody] SaveEssenceLoadoutDto request) =>
        await Mediator.Send(new SaveEssenceLoadoutCommand(CurrentCharacterGuid, request with { Id = loadoutId }));

    [HttpPost("loadouts/{loadoutId:guid}/activate")]
    public async Task<ActionResult<Response<ResponseMessageDto>>> ActivateLoadout(Guid loadoutId) =>
        await Mediator.Send(new ActivateEssenceLoadoutCommand(CurrentCharacterGuid, loadoutId));

    [HttpDelete("loadouts/{loadoutId:guid}")]
    public async Task<ActionResult<Response<ResponseMessageDto>>> DeleteLoadout(Guid loadoutId) =>
        await Mediator.Send(new DeleteEssenceLoadoutCommand(CurrentCharacterGuid, loadoutId));
}
