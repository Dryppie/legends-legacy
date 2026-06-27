using Application.UseCases.CombatStyles.Commands.ActivateCombatStyle;
using Application.UseCases.CombatStyles.Commands.RankUpCombatStyleNode;
using Application.UseCases.CombatStyles.Commands.ResetCombatStyleTree;
using Application.UseCases.CombatStyles.Commands.SelectCombatStyleFocus;
using Application.UseCases.CombatStyles.Dtos;
using Application.UseCases.CombatStyles.Queries.GetCombatBuildPreview;
using Application.UseCases.CombatStyles.Queries.GetCombatStyles;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Route("api/v{version:apiVersion}/combat-styles")]
public sealed class CombatStylesController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<CombatStylesOverviewDto>> GetCombatStyles() =>
        await Mediator.Send(new GetCombatStylesQuery(CurrentCharacterGuid));

    [HttpPost("{styleId}/activate")]
    public async Task<ActionResult<Response<ActivateCombatStyleResponseDto>>> Activate(string styleId) =>
        await Mediator.Send(new ActivateCombatStyleCommand(CurrentCharacterGuid, styleId));

    [HttpPost("{styleId}/focus/{focusId}/select")]
    public async Task<ActionResult<Response<CombatStyleDto>>> SelectFocus(string styleId, string focusId) =>
        await Mediator.Send(new SelectCombatStyleFocusCommand(CurrentCharacterGuid, styleId, focusId));

    [HttpPost("{styleId}/tree/nodes/{nodeId}/rank-up")]
    public async Task<ActionResult<Response<CombatStyleMutationResponseDto>>> RankUpNode(string styleId, string nodeId) =>
        await Mediator.Send(new RankUpCombatStyleNodeCommand(CurrentCharacterGuid, styleId, nodeId));

    [HttpPost("{styleId}/tree/reset")]
    public async Task<ActionResult<Response<CombatStyleMutationResponseDto>>> ResetTree(string styleId) =>
        await Mediator.Send(new ResetCombatStyleTreeCommand(CurrentCharacterGuid, styleId));

    [HttpGet("build-preview")]
    public async Task<ActionResult<CombatBuildPreviewDto>> GetBuildPreview() =>
        await Mediator.Send(new GetCombatBuildPreviewQuery(CurrentCharacterGuid));
}
