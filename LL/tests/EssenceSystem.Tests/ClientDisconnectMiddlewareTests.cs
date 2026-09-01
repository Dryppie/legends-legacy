using API.LL.Common;
using Microsoft.AspNetCore.Http;

namespace EssenceSystem.Tests;

public sealed class ClientDisconnectMiddlewareTests
{
    [Fact]
    public async Task Aborted_request_cancellation_is_treated_as_client_disconnect()
    {
        using var requestCancellation = new CancellationTokenSource();
        requestCancellation.Cancel();
        var context = new DefaultHttpContext
        {
            RequestAborted = requestCancellation.Token
        };
        var middleware = new ClientDisconnectMiddleware(
            _ => throw new OperationCanceledException(requestCancellation.Token));

        await middleware.InvokeAsync(context);

        Assert.Equal(
            ClientDisconnectMiddleware.ClientClosedRequestStatusCode,
            context.Response.StatusCode);
    }

    [Fact]
    public async Task Unrelated_cancellation_is_not_swallowed()
    {
        var context = new DefaultHttpContext();
        var middleware = new ClientDisconnectMiddleware(
            _ => throw new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => middleware.InvokeAsync(context));
    }
}
