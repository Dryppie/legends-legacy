using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[AllowAnonymous]
public class TimeSyncController : BaseController
{
    /// <summary>
    /// Returns the current server time as Unix epoch milliseconds.
    /// </summary>
    [HttpGet()]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ProducesResponseType(typeof(long), StatusCodes.Status200OK)]
    public ActionResult<long> GetCurrentTime() => Ok(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
}
