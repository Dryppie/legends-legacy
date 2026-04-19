using System.Security.Cryptography;
using System.Text;
using Domain.Models.Users;

namespace Persistence.LL.Repositories.Users;
public class Sha256TokenHasher : ITokenHasher
{
    public string Hash(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}