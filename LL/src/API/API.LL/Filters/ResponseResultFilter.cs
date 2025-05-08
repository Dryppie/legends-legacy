using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Common.Primitives;

namespace API.LL.Filters;

public sealed class ResponseResultFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        switch (context.Result)
        {
            // Controller returned Response<T> directly
            case ObjectResult { Value: { } value } obj when IsResponse(value):
                context.Result = ToActionResult(value);
                break;

            // Minimal‑API / Endpoint returned Response<T> via "Results.Extensions"
            case IResult result when IsResponse(result):
                context.Result = ToActionResult(Unwrap(result));
                break;
        }

        return next();
    }

    /* ------------- helpers ----------------- */

    private static bool IsResponse(object o)
        => o.GetType().IsGenericType &&
           o.GetType().GetGenericTypeDefinition() == typeof(Response<>);

    private static IActionResult ToActionResult(object response)
    {
        var isSuccess = (bool)response.GetType().GetProperty(nameof(Response<int>.IsSuccess))!
                                        .GetValue(response)!;

        return isSuccess
            ? new OkObjectResult(response.GetType().GetProperty(nameof(Response<int>.Data))!
                                            .GetValue(response))
            : new BadRequestObjectResult(response.GetType().GetProperty(nameof(Response<int>.ErrorMessage))!
                                                    .GetValue(response));
    }

    private static object Unwrap(IResult result) =>
        ((dynamic)result).Value;   // minimal‑API Result<T> → Response<T>
}