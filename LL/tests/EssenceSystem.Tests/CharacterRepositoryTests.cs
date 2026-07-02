using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Entities.Characters;

namespace EssenceSystem.Tests;

public sealed class CharacterRepositoryTests
{
    [Fact]
    public async Task CreateCharacterAsync_creates_full_arena_ticket_status()
    {
        await using var db = CreateDb();
        var repository = new CharacterRepository(db);

        var character = await repository.CreateCharacterAsync(
            Guid.NewGuid(),
            "ArenaNewbie",
            CancellationToken.None);
        await db.SaveChangesAsync();

        var tickets = await db.ArenaTicketStatus.SingleAsync(
            x => x.CharacterId == character.Id);
        Assert.Equal(5, tickets.CurrentTickets);
        Assert.Equal(5, tickets.MaxTickets);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }
}
