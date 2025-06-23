using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[AllowAnonymous]
public class TimeSyncController : BaseController
{
    /// <summary>
    /// Returns the current server time in UTC format.
    /// </summary>
    [HttpGet()]
    [ProducesResponseType(typeof(DateTimeOffset), StatusCodes.Status200OK)]
    public ActionResult<DateTimeOffset> GetCurrentTime() => Ok(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
}
