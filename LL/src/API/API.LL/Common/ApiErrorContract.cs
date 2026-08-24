using Microsoft.AspNetCore.Mvc;

namespace API.LL.Common;

public static class ApiErrorContract
{
    public const string BusinessCategory = "business";
    public const string ConflictCategory = "conflict";
    public const string SystemCategory = "system";

    public static ProblemDetails Create(
        HttpContext context,
        int status,
        string title,
        string message,
        string code,
        string category)
    {
        var details = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = message,
            Instance = context.Request.Path
        };

        AddExtensions(details, context, code, category, message);
        return details;
    }

    public static void Enrich(ProblemDetails details, HttpContext context)
    {
        var status = details.Status ?? context.Response.StatusCode;
        var defaults = GetDefaults(status);
        var existingCode = GetStringExtension(details, "code");
        var sanitizeUnexpectedFailure =
            status >= StatusCodes.Status500InternalServerError &&
            existingCode is null;
        var message = sanitizeUnexpectedFailure
            ? defaults.Message
            : details.Detail ?? defaults.Message;

        details.Status ??= status;
        if (sanitizeUnexpectedFailure)
        {
            details.Title = defaults.Title;
            details.Detail = message;
        }
        else
        {
            details.Title ??= defaults.Title;
            details.Detail ??= message;
        }
        details.Instance ??= context.Request.Path;

        AddExtensions(
            details,
            context,
            existingCode ?? defaults.Code,
            GetStringExtension(details, "category") ?? defaults.Category,
            GetStringExtension(details, "message") ?? message);
    }

    private static void AddExtensions(
        ProblemDetails details,
        HttpContext context,
        string code,
        string category,
        string message)
    {
        details.Extensions["code"] = code;
        details.Extensions["category"] = category;
        details.Extensions["message"] = message;
        details.Extensions["requestId"] =
            RequestLoggingMiddleware.GetRequestId(context);
    }

    private static string? GetStringExtension(ProblemDetails details, string key) =>
        details.Extensions.TryGetValue(key, out var value)
            ? value as string
            : null;

    private static ErrorDefaults GetDefaults(int status) => status switch
    {
        StatusCodes.Status400BadRequest => new(
            "bad_request",
            "validation",
            "Bad request",
            "The request was invalid."),
        StatusCodes.Status401Unauthorized => new(
            "authentication_required",
            "authentication",
            "Authentication required",
            "Authentication is required to perform this action."),
        StatusCodes.Status403Forbidden => new(
            "forbidden",
            "authorization",
            "Forbidden",
            "You are not allowed to perform this action."),
        StatusCodes.Status404NotFound => new(
            "not_found",
            "resource",
            "Not found",
            "The requested resource was not found."),
        StatusCodes.Status409Conflict => new(
            "conflict",
            ConflictCategory,
            "Conflict",
            "The request conflicts with the current state."),
        StatusCodes.Status500InternalServerError => new(
            "unexpected_error",
            SystemCategory,
            "Unexpected error",
            "An unexpected error occurred."),
        >= StatusCodes.Status500InternalServerError => new(
            "unexpected_error",
            SystemCategory,
            "Unexpected error",
            "An unexpected error occurred."),
        _ => new(
            "http_error",
            "http",
            "Request failed",
            "The request could not be completed.")
    };

    private sealed record ErrorDefaults(
        string Code,
        string Category,
        string Title,
        string Message);
}
