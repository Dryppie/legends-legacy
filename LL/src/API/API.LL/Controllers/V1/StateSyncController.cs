using Application.Interfaces.Services.LL;
using Application.WebSockets.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

public sealed class StateSyncController(IStateSyncService stateSyncService) : BaseController
{
    [HttpGet("checkpoint")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ProducesResponseType(typeof(StateSyncCheckpoint), StatusCodes.Status200OK)]
    public async Task<ActionResult<StateSyncCheckpoint>> GetCheckpoint(CancellationToken cancellationToken) =>
        Ok(await stateSyncService.GetCheckpointAsync(CurrentCharacterGuid, cancellationToken));
}
