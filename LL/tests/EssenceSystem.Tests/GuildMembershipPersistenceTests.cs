using Domain.Models.Guilds;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;

namespace EssenceSystem.Tests;

public sealed class GuildMembershipPersistenceTests
{
    [Fact]
    public void Model_allows_only_one_guild_membership_per_character()
    {
        using var db = CreateDbContext();
        var entityType = db.Model.FindEntityType(typeof(GuildMember));

        Assert.NotNull(entityType);
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.IsUnique
                     && index.Properties.Select(property => property.Name)
                         .SequenceEqual([nameof(GuildMember.CharacterId)]));
    }

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }
}
