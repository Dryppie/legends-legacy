using Common.Primitives;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Extensions;

/// <summary>
/// Converts <c>Response&lt;T&gt;</c> ➜ <c>IActionResult</c>.
/// </summary>
public static class ResponseHttpExtensions
{
    public static IActionResult ToActionResult<T>(this Response<T> r)
        => r.IsSuccess
           ? (r.Data is Unit ? new OkResult()
                              : new OkObjectResult(r.Data))
           : new ObjectResult(new ProblemDetails
           {
               Title = "Error",
               Detail = r.ErrorMessage,
               Status = StatusCodes.Status400BadRequest
           })
           { StatusCode = StatusCodes.Status400BadRequest };
}