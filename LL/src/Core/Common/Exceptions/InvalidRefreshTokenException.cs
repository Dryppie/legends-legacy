namespace Common.Exceptions;
public class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException() : base()
    {
    }

    public InvalidRefreshTokenException(string? message) : base(message)
    {
    }

    public InvalidRefreshTokenException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
