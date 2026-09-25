using Application.UseCases.Analytics.Commands.RecordActivityDay;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers;

public sealed class ActivityController : BaseController
{
    [HttpPost]
    public async Task<ActionResult<Response<bool>>> Record(CancellationToken ct) =>
        Ok(await Mediator.Send(new RecordActivityDayCommand(CurrentUserId), ct));
}
