namespace Application.Common.Responses;
public class Response<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static Response<T> Success(T data)
    {
        return new Response<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    public static Response<T> Fail(string errorMessage)
    {
        return new Response<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}