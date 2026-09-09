using Application.UseCases.CombatStyles.Commands.SelectCombatStyle;
using Application.UseCases.CombatStyles.Dtos;
using Application.UseCases.CombatStyles.Queries.GetCombatStyles;
using Application.UseCases.CombatStyles.Queries.PreviewCombatStyle;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Route("api/v{version:apiVersion}/combat-styles")]
public sealed class CombatStylesController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<CombatStyleOverviewDto>> Get() =>
        await Mediator.Send(new GetCombatStylesQuery(CurrentCharacterGuid));

    [HttpPost("preview")]
    public async Task<ActionResult<CombatStyleOverviewDto>> Preview([FromBody] CombatStyleSelectionDto request) =>
        await Mediator.Send(new PreviewCombatStyleQuery(CurrentCharacterGuid, request));

    [HttpPut("selection")]
    public async Task<ActionResult<Response<CombatStyleOverviewDto>>> Select([FromBody] CombatStyleSelectionDto request) =>
        await Mediator.Send(new SelectCombatStyleCommand(CurrentCharacterGuid, request));

}
