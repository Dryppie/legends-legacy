namespace Domain.Models.Users;
public interface IExternalLoginRepository
{
    Task<ExternalLogin?> FindAsync(AuthProvider provider, string providerUserId, CancellationToken cancellationToken);
    void Add(ExternalLogin login);
}