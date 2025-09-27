using MediatR;

namespace Application.MediatR.Behaviors;
public sealed class ExceptionToResponseBehaviour<TRequest, TResponse> :
    IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            // We know the handler should return Response<something>
            // Build a failed instance via reflection (generic type unknown here).
            var responseType = typeof(TResponse);
            var failMethod = responseType.GetMethod("Fail", [typeof(string)])!;
            return (TResponse)failMethod.Invoke(null, [ex.Message])!;
        }
    }
}