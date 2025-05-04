using System.Security.Cryptography;
using System.Text;
using Domain.Models.Users;

namespace Persistence.LL.Repositories.Users;
public sealed class Sha256TokenHasher : ITokenHasher
{
    private static readonly SHA256 _sha = SHA256.Create();
    public string Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = _sha.ComputeHash(bytes);
        return Convert.ToHexString(hash); // .NET 8
    }
}