namespace Common.Primitives;
public class Response<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string ErrorMessage { get; init; } =  string.Empty;

    public static Response<T> Success(T data) =>
        new() { IsSuccess = true, Data = data };

    public static Response<T> Fail(string error) =>
        new() { IsSuccess = false, ErrorMessage = error };
}