using Application.UseCases.LootHistory.Commands.ClearLootHistory;
using Application.UseCases.LootHistory.Dtos;
using Application.UseCases.LootHistory.Queries.GetLootHistory;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
public sealed class LootHistoryController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<Response<IReadOnlyList<LootHistoryEntryDto>>>> Get() =>
        await Mediator.Send(new GetLootHistoryQuery(CurrentCharacterGuid));

    [HttpDelete]
    public async Task<ActionResult<Response<int>>> Clear() =>
        await Mediator.Send(new ClearLootHistoryCommand(CurrentCharacterGuid));
}
