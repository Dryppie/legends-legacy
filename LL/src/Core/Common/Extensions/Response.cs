using Common.Primitives;

namespace Common.Extensions;
public static class Response
{
    public static Response<T> FromNullable<T>(T? value, string errorMessage)
        where T : class
        => value is not null ? Response<T>.Success(value)
                             : Response<T>.Fail(errorMessage);
}