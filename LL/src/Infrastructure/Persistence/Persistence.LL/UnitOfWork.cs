using Microsoft.EntityFrameworkCore;
using Persistence.LL.Interfaces;

namespace Persistence.LL;
public class UnitOfWork : IUnitOfWork
{
    private readonly LLDbContext _context;

    public UnitOfWork(IDbContextFactory<LLDbContext> dbContextFactory)
    {
        _context = dbContextFactory.CreateDbContext();
    }

    public LLDbContext Context => _context;

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}