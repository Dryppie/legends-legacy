using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Common.Randomness;

/// <summary>
/// Stable cross-process identities for replayable gameplay resolution.
/// Callers must supply invariant, canonical component strings.
/// </summary>
public static class StableRandom
{
    public static int Seed(params string[] components)
    {
        var canonical = string.Join('\u001f', components);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return BinaryPrimitives.ReadInt32LittleEndian(hash);
    }

    public static Guid Guid(params string[] components)
    {
        var canonical = string.Join('\u001f', components);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return new Guid(hash.AsSpan(0, 16));
    }
}
