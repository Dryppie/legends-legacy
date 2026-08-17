using Application.Interfaces;
using Domain.Models.Chats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;

namespace Persistence.Chat;
public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options), IDbContext
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ChangeTracker.Entries<ChatModerationAction>()
            .Any(x => x.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "Chat moderation audit entries are append-only and cannot be modified or deleted.");
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ExecuteSqlRawAsync(string sql, CancellationToken token = default, params object[] sqlParams)
    {
        return await Database.ExecuteSqlRawAsync(sql, sqlParams);
    }

    public EntityEntry<TEntity> GetEntry<TEntity>(TEntity entity) where TEntity : class
        => Entry(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            SetupSqlite(modelBuilder);
        }
    }

    private static void SetupSqlite(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(DateTimeOffset)
                                                                           || p.PropertyType ==
                                                                           typeof(DateTimeOffset?));
            foreach (var property in properties)
            {
                builder
                    .Entity(entityType.Name)
                    .Property(property.Name)
                    .HasConversion(new DateTimeOffsetToBinaryConverter());
            }
        }
    }

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChatRestriction> ChatRestrictions => Set<ChatRestriction>();
    public DbSet<ChatModerationAction> ChatModerationActions => Set<ChatModerationAction>();
}

public class LLDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory() + "\\..\\API.Chat";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("LLChatDB");

        DbContextOptionsBuilder<ChatDbContext> optionsBuilder = new();

        var timeout = configuration.GetSection("Database").GetValue<int>("TimeoutInSeconds");
        optionsBuilder.UseNpgsql(connectionString, sqlServerOptions => sqlServerOptions.CommandTimeout(timeout));

        return new(optionsBuilder.Options);
    }
}
