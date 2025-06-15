using Application.WebSockets.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
public class GameHubController : BaseController
{
    private readonly GameSocketHandler _handler;

    public GameHubController(GameSocketHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("game")]
    public async Task Get(CancellationToken ct)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await _handler.RunAsync(webSocket, CurrentCharacterGuid, ct);      // fully delegated
    }
}
