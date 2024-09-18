using System.Diagnostics.CodeAnalysis;

namespace Common.Exceptions;
[Serializable]
public class NotFoundException : Exception
{
    public NotFoundException() : base("Entity was not found.")
    {
    }

    public NotFoundException(string entity, object key) : base($"Entity \"{entity}\". {key} was not found.")
    {
    }

    public NotFoundException(string entity, object key, Exception inner) : base($"Entity \"{entity}\". {key} was not found.", inner)
    {
    }

    public static void ThrowIfNull([NotNull] object? obj, string entity, object key)
    {
        if (obj == null)
        {
            throw new NotFoundException(entity, key);
        }
    }
}
