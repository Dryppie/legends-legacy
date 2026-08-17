using Domain.Models.Chats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Application.Interfaces;
public interface IDbContext
{
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<ChatRestriction> ChatRestrictions { get; }
    DbSet<ChatModerationAction> ChatModerationActions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Execute raw sql. Never use string interpolation to embed values as this can cause sql injection
    /// Instead parse extra args as sqlParams
    /// </summary>
    /// <param name="sql"></param>
    /// <param name="token"></param>
    /// <param name="sqlParams"></param>
    /// <returns></returns>
    Task<int> ExecuteSqlRawAsync(string sql, CancellationToken token = default, params object[] sqlParams);

    /// <summary>
    /// Exposes EF Core's Entry method to allow property state manipulation.
    /// </summary>
    EntityEntry<TEntity> GetEntry<TEntity>(TEntity entity) where TEntity : class;
}
