namespace Domain.Models.Users;
public interface IRefreshTokenRepository
{
    void Add(RefreshToken token);
    Task<RefreshToken?> FindAsync(string plaintext, CancellationToken cancellationToken);
}