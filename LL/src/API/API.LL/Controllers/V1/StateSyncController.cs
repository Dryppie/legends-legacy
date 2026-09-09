using API.LL.Common;
using Application.Interfaces.Services.LL;
using Application.WebSockets.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

public sealed class StateSyncController(IStateSyncService stateSyncService) : BaseController
{
    [HttpGet("checkpoint")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ProducesResponseType(typeof(StateSyncCheckpoint), StatusCodes.Status200OK)]
    public async Task<ActionResult<StateSyncCheckpoint>> GetCheckpoint(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await stateSyncService.GetCheckpointAsync(CurrentCharacterGuid, cancellationToken));
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            // Refreshing the page aborts the previous checkpoint query. Handle it
            // before it crosses MVC's async boundary, where the debugger can
            // report it as user-unhandled despite ClientDisconnectMiddleware.
            return StatusCode(ClientDisconnectMiddleware.ClientClosedRequestStatusCode);
        }
    }
}
