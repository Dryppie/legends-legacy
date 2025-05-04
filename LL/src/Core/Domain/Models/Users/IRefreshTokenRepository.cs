namespace Domain.Models.Users;
public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);
    Task<RefreshToken?> FindAsync(string plaintext, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}