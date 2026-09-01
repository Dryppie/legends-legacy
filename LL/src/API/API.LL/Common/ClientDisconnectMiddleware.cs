namespace API.LL.Common;

public sealed class ClientDisconnectMiddleware(RequestDelegate next)
{
    // Nginx's conventional status for a request whose client disconnected.
    // The response normally cannot be delivered, but setting it keeps request
    // diagnostics from classifying an expected cancellation as a server error.
    public const int ClientClosedRequestStatusCode = 499;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = ClientClosedRequestStatusCode;
            }
        }
    }
}
