namespace Persistence.LL.Interfaces;
public interface IUnitOfWork : IDisposable
{
    LLDbContext Context { get; }
    Task<int> SaveChangesAsync();
}

