namespace Common.Primitives;
public class Response<T>
{
    public const string DefaultErrorCode = "business_rule_rejected";

    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public string ErrorCode { get; init; } = DefaultErrorCode;
    public bool IsConflict { get; init; }

    public static Response<T> Success(T data) =>
        new() { IsSuccess = true, Data = data };

    public static Response<T> Fail(string error) =>
        Fail(error, DefaultErrorCode);

    public static Response<T> Fail(string error, string errorCode) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = error,
            ErrorCode = string.IsNullOrWhiteSpace(errorCode)
                ? DefaultErrorCode
                : errorCode
        };

    public static Response<T> Conflict(string error, string errorCode) =>
        new()
        {
            IsSuccess = false,
            IsConflict = true,
            ErrorMessage = error,
            ErrorCode = string.IsNullOrWhiteSpace(errorCode)
                ? DefaultErrorCode
                : errorCode
        };
}
