using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[AllowAnonymous]
public class TimeSyncController : BaseController
{
    private readonly TimeProvider _timeProvider;

    public TimeSyncController(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Returns the current server time as Unix epoch milliseconds.
    /// </summary>
    [HttpGet()]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ProducesResponseType(typeof(long), StatusCodes.Status200OK)]
    public ActionResult<long> GetCurrentTime() => Ok(_timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
}
