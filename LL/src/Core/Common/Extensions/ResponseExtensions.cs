using Common.Primitives;

namespace Common.Extensions;
public static class ResponseExtensions
{
    public static bool Failed<T>(this Response<T> response, out string error)
    {
        error = response.ErrorMessage ?? "Unknown error";
        return !response.IsSuccess;
    }
}