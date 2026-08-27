using System.Security.Cryptography;
using System.Text;

namespace Services.LL.Combat.Profiles;

internal static class CombatCharacterProfileIdentity
{
    public static string CreateStableId(string prefix, params string[] parts)
    {
        var hash = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(string.Join('|', parts)))).ToLowerInvariant();
        return $"{prefix}-{hash[..16]}";
    }

    public static Guid CreateDeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }
}
