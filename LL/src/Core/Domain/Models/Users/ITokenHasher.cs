namespace Domain.Models.Users;
public interface ITokenHasher
{
    string Hash(string input);
}