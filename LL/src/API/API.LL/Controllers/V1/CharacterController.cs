using Application.UseCases.Characters.Dtos;
using Application.UseCases.Characters.Queries.GetCharacter;
using Application.UseCases.Characters.Queries.GetCharacterIdByName;
using Application.UseCases.Characters.Queries.GetCharacterOverview;
using Application.UseCases.Characters.Queries.GetCharacterOverviewByName;
using Application.UseCases.Characters.Queries.GetPowerRating;
using Application.UseCases.Characters.Queries.SearchCharacterNames;
using Application.UseCases.Characters.Commands.WireCinders;
using Application.Interfaces.Services.LL.PowerRatings;
using API.LL.Common;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
public class CharacterController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<Response<CharacterDto>>> Get() =>
        await Mediator.Send(new GetCharacterQuery(CurrentUserId));

    [HttpGet("Overview")]
    public async Task<ActionResult<Response<CharacterOverviewDto>>> Overview() =>
        await Mediator.Send(new GetCharacterOverviewQuery(CurrentCharacterGuid));

    [HttpGet("Power")]
    public async Task<ActionResult<PowerRatingSnapshot>> Power() =>
        await Mediator.Send(new GetPowerRatingQuery(CurrentCharacterGuid));

    [HttpGet("Search")]
    public async Task<ActionResult<Response<CharacterOverviewDto>>> Search([FromQuery] string name)
    {
        var result = await Mediator.Send(new GetCharacterOverviewByNameQuery(name));
        if (!result.IsSuccess || result.Data == null)
            return NotFound();

        return Ok(result.Data);
    }

    [HttpGet("ResolveName")]
    public async Task<ActionResult<Guid?>> ResolveName([FromQuery] string name)
    {
        var result = await Mediator.Send(new GetCharacterIdByNameQuery(name));
        if (!result.IsSuccess || result.Data == null)
            return NotFound();

        return Ok(result.Data);
    }

    [HttpGet("Suggestions")]
    public async Task<ActionResult<IReadOnlyList<string>>> Suggestions([FromQuery] string query) =>
        Ok(await Mediator.Send(new SearchCharacterNamesQuery(CurrentCharacterGuid, query)));

    [HttpPost("Wire")]
    [Authorize(Policy = AuthorizationPolicies.MultiplayerAllowed)]
    public async Task<ActionResult<Response<WireCindersResponseDto>>> Wire(
        [FromBody] WireCurrencyRequestDto request) =>
        await Mediator.Send(new WireCindersCommand(
            CurrentCharacterGuid,
            request.RecipientName,
            request.Amount,
            request.Currency));
}
