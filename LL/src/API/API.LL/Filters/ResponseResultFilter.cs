using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Common.Primitives;
using API.LL.Common;

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
                context.Result = ToActionResult(value, context.HttpContext);
                break;

            // Minimal‑API / Endpoint returned Response<T> via "Results.Extensions"
            case IResult result when IsResponse(result):
                context.Result = ToActionResult(Unwrap(result), context.HttpContext);
                break;
        }

        return next();
    }

    /* ------------- helpers ----------------- */

    private static bool IsResponse(object o)
        => o.GetType().IsGenericType &&
           o.GetType().GetGenericTypeDefinition() == typeof(Response<>);

    private static IActionResult ToActionResult(
        object response,
        HttpContext httpContext)
    {
        var responseType = response.GetType();
        var isSuccess = (bool)responseType
            .GetProperty(nameof(Response<int>.IsSuccess))!
            .GetValue(response)!;

        if (isSuccess)
        {
            return new OkObjectResult(
                responseType.GetProperty(nameof(Response<int>.Data))!
                    .GetValue(response));
        }

        var message = (string)responseType
            .GetProperty(nameof(Response<int>.ErrorMessage))!
            .GetValue(response)!;
        var code = (string)responseType
            .GetProperty(nameof(Response<int>.ErrorCode))!
            .GetValue(response)!;
        var isConflict = (bool)responseType
            .GetProperty(nameof(Response<int>.IsConflict))!
            .GetValue(response)!;
        var status = isConflict
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status400BadRequest;

        var problem = ApiErrorContract.Create(
            httpContext,
            status,
            isConflict ? "Conflict" : "Action rejected",
            message,
            code,
            isConflict
                ? ApiErrorContract.ConflictCategory
                : ApiErrorContract.BusinessCategory);
        return new ObjectResult(problem)
        {
            StatusCode = status
        };
    }

    private static object Unwrap(IResult result) =>
        ((dynamic)result).Value;   // minimal‑API Result<T> → Response<T>
}
